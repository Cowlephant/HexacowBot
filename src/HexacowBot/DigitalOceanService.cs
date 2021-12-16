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

		slugPrices = (await client.Sizes.GetAll())
			.Select(s => (s.Slug, $"${s.PriceMonthly}"))
			.ToHashSet<(string, string)>();
	}

	public async Task<bool> ResizeDroplet(string sizeSlug)
	{
		// TODO: Check if there are running servers first and abort and notify if so
		// Attempt to shut down droplet first, failing if it fails
		var success = await StopDroplet();
		if (!success)
		{
			return false;
		}

		// Attempt to resize the server, failing if it fails
		var action = await client.DropletActions.Resize(DropletId, sizeSlug, resizeDisk: false);
		success = await WaitForActionToComplete(action, pollingInterval: 5000, -1);
		if (!success)
		{
			return false;
		}

		// Finally attempt to start the server back up, failing if it fails
		return await StartDroplet();
	}

	public async Task<Size> GetDropletSize()
	{
		return (await client.Droplets.Get(DropletId)).Size;
	}

	public string GetSlugMonthlyCost(string sizeSlug)
	{
		return slugPrices.First(s => s.Item1 == sizeSlug).Item2;
	}

	public async Task<string> GetMonthToDateBalance()
	{
		var balance = await client.BalanceClient.Get();
		return balance.MonthToDateBalance;
	}

	public async Task<bool> StopDroplet()
	{
		var action = await client.DropletActions.Shutdown(DropletId);
		var success = await WaitForActionToComplete(action);

		// If shutdown wasn't successful after interval, attempt a power off
		if (!success)
		{
			action = await client.DropletActions.PowerOff(DropletId);
			success = await WaitForActionToComplete(action, actionPollInterval);
		}

		return success;
	}

	public async Task<bool> StartDroplet()
	{
		var action = await client.DropletActions.PowerOn(DropletId);
		return await WaitForActionToComplete(action);
	}

	public async Task<bool> RestartDroplet()
	{
		var action = await client.DropletActions.Reboot(DropletId);
		return await WaitForActionToComplete(action);
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
