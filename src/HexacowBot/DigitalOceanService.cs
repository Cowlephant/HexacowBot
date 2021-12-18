using DigitalOcean.API;
using DigitalOcean.API.Models.Responses;

namespace HexacowBot;

public sealed class DigitalOceanService
{
	private const int shutdownTimeout = 90;
	private const int actionPollInterval = 1000;

	private readonly DigitalOceanClient client;
	private readonly IConfiguration configuration;

	public long DropletId { get; private set; }
	public string DropletName { get; private set; } = "Not Named - Rename me in config";
	public bool IsBusy { get; private set; }

	private HashSet<(string, Size)> slugSizes = null!;
	public IEnumerable<(string, Size)> SlugSizes => SlugSizes.AsEnumerable();

	private HashSet<(string, string)> slugPrices = null!;
	public IEnumerable<(string, string)> SlugPrices => slugPrices.AsEnumerable();

	public DigitalOceanService(IConfiguration configuration, DigitalOceanClient client)
	{
		this.configuration = configuration;
		this.client = client;

		Initialize();
	}

	private async void Initialize()
	{
		DropletId = long.Parse(configuration["DigitalOcean:DropletId"]);
		DropletName = configuration["DigitalOcean:DropletName"];

		slugSizes = (await client.Sizes.GetAll())
			.Select(s => (s.Slug, s))
			.ToHashSet<(string, Size)>();
	}

	public async Task<Size> GetDropletSize()
	{
		return (await client.Droplets.Get(DropletId)).Size;
	}

	public Size GetSlugSize(string sizeSlug)
	{
		return slugSizes.FirstOrDefault(s => s.Item1 == sizeSlug).Item2;
	}

	public async Task<string> GetMonthToDateBalance()
	{
		var balance = await client.BalanceClient.Get();
		return $"${balance.MonthToDateBalance}";
	}

	public async Task<ServerActionResult> StartDroplet()
	{
		var action = await client.DropletActions.PowerOn(DropletId);
		var success = await WaitForActionToComplete(action);

		if (success)
		{
			return new ServerActionResult(
				true, $"The server ({DropletName}) was successfully started.", LogLevel.Information);
		}
		else
		{
			return new ServerActionResult(
				false, $"The server ({DropletName}) start operation failed.", LogLevel.Critical);
		}
	}

	public async Task<ServerActionResult> StopDroplet()
	{
		var action = await client.DropletActions.Shutdown(DropletId);
		var shutdownFailed = !(await WaitForActionToComplete(action));

		// If shutdown wasn't successful after interval, attempt a power off
		if (shutdownFailed)
		{
			action = await client.DropletActions.PowerOff(DropletId);
			var powerOffFailed = !(await WaitForActionToComplete(action, actionPollInterval));

			if (powerOffFailed)
			{
				return new ServerActionResult(
					false, $"The server ({DropletName}) stop operation failed.", LogLevel.Critical);
			}

			return new ServerActionResult(
				true, $"The server ({DropletName}) was stopped ungracefully.", LogLevel.Warning);
		}

		return new ServerActionResult(
			true, $"The server ({DropletName}) was successfully stopped.", LogLevel.Information);
	}

	public async Task<ServerActionResult> RestartDroplet()
	{
		var action = await client.DropletActions.Reboot(DropletId);
		var success = await WaitForActionToComplete(action);

		if (success)
		{
			return new ServerActionResult(
				true, $"The server ({DropletName}) was successfully restarted.", LogLevel.Information);
		}
		else
		{
			return new ServerActionResult(
				false, $"The server ({DropletName}) reboot operation failed.", LogLevel.Critical);
		}
	}

	public async Task PowerCycleDroplet(System.Action? callback = null)
	{
		var action = await client.DropletActions.PowerCycle(DropletId);
		await WaitForActionToComplete(action);

		if (callback is not null)
		{
			callback();
		}
	}

	//public async Task HibernateDropnlet()

	public async Task<ServerActionResult> ResizeDroplet(string sizeSlug)
	{
		var currentSize = await GetDropletSize();
		if (currentSize.Slug == sizeSlug)
		{
			return new ServerActionResult(
				false, $"The server ({DropletName}) is already this size.", LogLevel.Information);
		}

		// TODO: Check if there are running servers first and abort and notify if so
		// Attempt to shut down droplet first, failing if it fails
		var stopFailed = !(await StopDroplet()).Success;
		if (stopFailed)
		{
			return new ServerActionResult(
				false, $"The server ({DropletName}) could not be stopped while attempting to resize.", LogLevel.Critical);
		}

		// Attempt to resize the server, failing if it fails
		var action = await client.DropletActions.Resize(DropletId, sizeSlug, resizeDisk: false);
		var resizeFailed = !(await WaitForActionToComplete(action, pollingInterval: 5000, -1));
		if (resizeFailed)
		{
			return new ServerActionResult(
				false, $"The server ({DropletName}) resizing operation failed.", LogLevel.Critical);
		}

		// Finally attempt to start the server back up, failing if it fails
		var startFailed = !(await StartDroplet()).Success;
		if (startFailed)
		{
			return new ServerActionResult(
				false, $"The server ({DropletName}) could not be started again after resizing.", LogLevel.Critical);
		}

		return new ServerActionResult(
			true, $"The server ({DropletName}) was successfully resized.", LogLevel.Information);
	}

	private async Task<bool> WaitForActionToComplete(
		DigitalOcean.API.Models.Responses.Action action,
		int pollingInterval = actionPollInterval,
		int pollingMaxTimes = 90)
	{
		var isTimeoutAllowed = pollingMaxTimes != -1;
		int timeoutCount = 0;
		while (Status(action.Status) != DropletActionStatus.Completed)
		{
			action = await client.Actions.Get(action.Id);

			Thread.Sleep(pollingInterval);
			timeoutCount++;

			// Fail if we've errored out
			if (Status(action.Status) == DropletActionStatus.Errored)
			{
				return false;
			}

			// Fail if we've timed out
			var isTimedOut = timeoutCount >= pollingMaxTimes;
			if (isTimeoutAllowed && isTimedOut)
			{
				return false;
			}
		}

		return true;
	}

	private DropletActionStatus Status(string status)
	{
		var dropletActionStatus = DropletActionStatus.FromName(status);
		return DropletActionStatus.FromName(status);
	}
}
