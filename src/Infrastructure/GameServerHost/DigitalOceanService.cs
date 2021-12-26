using DigitalOcean.API;
using DigitalOcean.API.Models.Responses;
using HexacowBot.Core.GameServerHost;
using System.Diagnostics;

namespace HexacowBot.Infrastructure.GameServerHost;

internal sealed class DigitalOceanService : IGameServerHost
{
	private const int shutdownTimeout = 90;
	private const int actionPollInterval = 1000;
	private ServerActionResult busyActionResult = new ServerActionResult(
		false, "The server is currently undergoing an operation. Please wait and try again.", TimeSpan.Zero, LogLevel.Information);

	private readonly DigitalOceanClient client;
	private readonly IConfiguration configuration;

	public long DropletId { get; private set; }
	public string ServerName { get; private set; } = "Not Named - Rename me in config";
	
	public bool IsBusy { get; private set; } = false;

	public ServerSize CurrentSize { get; private set; } = null!;
	public ServerSize HibernateSize { get; private set; } = null!;

	private HashSet<ServerSize> serverSizes = null!;
	public IEnumerable<ServerSize> ServerSizes => serverSizes.AsEnumerable();

	private HashSet<ServerSize> allowedSizes = null!;
	public IEnumerable<ServerSize> AllowedSizes => allowedSizes.AsEnumerable();

	public DigitalOceanService(IConfiguration configuration, DigitalOceanClient client)
	{
		this.configuration = configuration;
		this.client = client;

		Initialize();
	}

	private async void Initialize()
	{
		DropletId = long.Parse(configuration["DigitalOcean:DropletId"]);
		ServerName = configuration["DigitalOcean:DropletName"] ?? "Not Named - Rename me in config";

		serverSizes = (await client.Sizes.GetAll())
			.Select(s => s.ToServerSize())
			.ToHashSet();

		var allowedSlugs = configuration.GetSection("DigitalOcean:AllowedSlugs").Get<string[]>();
		allowedSizes = allowedSlugs.Select(allowed => serverSizes
			.First(size => allowed == size.Slug))
			.ToHashSet();

		CurrentSize = (await client.Droplets.Get(DropletId)).Size.ToServerSize();

		var hibernateSlug = configuration["DigitalOcean:HibernateSlug"];
		HibernateSize = serverSizes.First(s => s.Slug == hibernateSlug);

		var hibernateSlugNotAllowed = !(allowedSizes.Any(s => s == HibernateSize));
		if (hibernateSlugNotAllowed)
		{
			throw new GameServerHostException("Hibernate slug is not in configured list of allowed slugs.");
		}
	}

	public async Task<bool> CheckIsStarted()
	{
		var droplet = await GetDroplet();
		return DropletStatus.FromName(droplet.Status) == DropletStatus.Active;
	}

	public async Task<bool> CheckIsHibernating()
	{
		return await Task.FromResult(CurrentSize == HibernateSize);
	}

	public async Task<ServerSize> GetCurrentSizeAsync()
	{
		return (await client.Droplets.Get(DropletId)).Size.ToServerSize();
	}

	public async Task<ServerSize?> GetServerSizeAsync(string size)
	{
		return await Task.FromResult(serverSizes.FirstOrDefault(s => s.Slug == size));
	}

	public async Task<decimal> GetMonthToDateBalanceAsync()
	{
		var balance = await client.BalanceClient.Get();
		return decimal.Parse(balance.MonthToDateBalance);
	}

	// The Async methods are for accessing externally via IGameServer interface. The IsBusy check will notify
	// external users if the command cannot be completed at this time as it is executing another, without
	// interfering internally with the same commands being used in multi-step operations

	public async Task<ServerActionResult> StartServerAsync()
	{
		if (IsBusy)
		{
			return busyActionResult;
		}

		return await StartServerExecuteAsync();
	}

	public async Task<ServerActionResult> StopServerAsync()
	{
		if (IsBusy)
		{
			return busyActionResult;
		}

		return await StopServerExecuteAsync();
	}

	public async Task<ServerActionResult> RestartServerAsync()
	{
		if (IsBusy)
		{
			return busyActionResult;
		}

		return await RestartServerExecuteAsync();
	}

	public async Task<ServerActionResult> PowerCycleServerAsync()
	{
		if (IsBusy)
		{
			return busyActionResult;
		}

		return await PowerCycleServerExecuteAsync();
	}

	public async Task<ServerActionResult> ScaleServerAsync(ServerSize size)
	{
		if (IsBusy)
		{
			return busyActionResult;
		}

		return await ScaleServerExecuteAsync(size);
	}

	public async Task<ServerActionResult> HibernateServerAsync()
	{
		if (IsBusy)
		{
			return busyActionResult;
		}

		return await HibernateServerExecuteAsync();
	}

	private async Task<ServerActionResult> StartServerExecuteAsync()
	{
		BlockServerAccess();

		var droplet = await GetDroplet();
		var alreadyStarted = DropletStatus.FromName(droplet.Status) == DropletStatus.Active;
		if (alreadyStarted)
		{
			return new ServerActionResult(true,
				$"The server ({ServerName}) is already started.",
				TimeSpan.Zero,
				LogLevel.Information);
		}

		var stopwatch = new Stopwatch();
		stopwatch.Start();

		var action = await client.DropletActions.PowerOn(DropletId);

		var success = await WaitForActionToComplete(action);

		FreeServerAccess();
		stopwatch.Stop();

		if (success)
		{
			return new ServerActionResult(
				true, $"The server ({ServerName}) was successfully started.", stopwatch.Elapsed, LogLevel.Information);
		}
		else
		{
			return new ServerActionResult(
				false, $"The server ({ServerName}) start operation failed.", stopwatch.Elapsed, LogLevel.Critical);
		}
	}

	private async Task<ServerActionResult> StopServerExecuteAsync()
	{
		BlockServerAccess();

		var droplet = await GetDroplet();
		var alreadyOff = DropletStatus.FromName(droplet.Status) == DropletStatus.Off;
		if (alreadyOff)
		{
			return new ServerActionResult(true,
				$"The server ({ServerName}) is already stopped.",
				TimeSpan.Zero,
				LogLevel.Information);
		}

		var stopwatch = new Stopwatch();
		stopwatch.Start();

		var action = await client.DropletActions.Shutdown(DropletId);
		var shutdownFailed = !(await WaitForActionToComplete(action));

		// If shutdown wasn't successful after interval, attempt a power off
		if (shutdownFailed)
		{
			action = await client.DropletActions.PowerOff(DropletId);
			var powerOffFailed = !(await WaitForActionToComplete(action, actionPollInterval));

			FreeServerAccess();
			stopwatch.Stop();

			if (powerOffFailed)
			{
				return new ServerActionResult(
					false, $"The server ({ServerName}) stop operation failed.", stopwatch.Elapsed, LogLevel.Critical);
			}

			return new ServerActionResult(
				true, $"The server ({ServerName}) was stopped ungracefully.", stopwatch.Elapsed, LogLevel.Warning);
		}

		FreeServerAccess();
		stopwatch.Stop();

		return new ServerActionResult(
			true, $"The server ({ServerName}) was successfully stopped.", stopwatch.Elapsed, LogLevel.Information);
	}

	private async Task<ServerActionResult> RestartServerExecuteAsync()
	{
		BlockServerAccess();

		var stopwatch = new Stopwatch();
		stopwatch.Start();

		var action = await client.DropletActions.Reboot(DropletId);
		var success = await WaitForActionToComplete(action);

		FreeServerAccess();
		stopwatch.Stop();

		if (success)
		{
			return new ServerActionResult(
				true, $"The server ({ServerName}) was successfully restarted.", stopwatch.Elapsed, LogLevel.Information);
		}
		else
		{
			return new ServerActionResult(
				false, $"The server ({ServerName}) reboot operation failed.", stopwatch.Elapsed, LogLevel.Critical);
		}
	}

	private async Task<ServerActionResult> PowerCycleServerExecuteAsync()
	{
		BlockServerAccess();

		var stopwatch = new Stopwatch();
		stopwatch.Start();

		var action = await client.DropletActions.PowerCycle(DropletId);
		var success = await WaitForActionToComplete(action);

		FreeServerAccess();
		stopwatch.Stop();

		if (success)
		{
			return new ServerActionResult(
				true, $"The server ({ServerName}) was successfully power cycled.", stopwatch.Elapsed, LogLevel.Information);
		}
		else
		{
			return new ServerActionResult(
				false, $"The server ({ServerName}) power cycle operation failed.", stopwatch.Elapsed, LogLevel.Critical);
		}
	}

	private async Task<ServerActionResult> ScaleServerExecuteAsync(ServerSize size)
	{
		BlockServerAccess();

		var stopwatch = new Stopwatch();
		stopwatch.Start();

		var isDisallowedSlugSize = !(allowedSizes.Any(s => s.Slug == size.Slug));
		var currentSize = await GetCurrentSizeAsync();

		if (isDisallowedSlugSize)
		{
			FreeServerAccess();
			stopwatch.Stop();

			return new ServerActionResult(false,
				$"The specified slug size is not a valid size for server ({ServerName})",
				stopwatch.Elapsed,
				LogLevel.Warning);
		}
		if (currentSize.Slug == size.Slug)
		{
			FreeServerAccess();
			stopwatch.Stop();

			return new ServerActionResult(
				true, $"The server ({ServerName}) is already this size.", stopwatch.Elapsed, LogLevel.Information);
		}

		// TODO: Check if there are running servers first and abort and notify if so
		// Attempt to shut down droplet first, failing if it fails
		var stopFailed = !(await StopServerExecuteAsync()).Success;
		if (stopFailed)
		{
			FreeServerAccess();
			stopwatch.Stop();

			return new ServerActionResult(false,
				$"The server ({ServerName}) could not be stopped while attempting to resize.",
				stopwatch.Elapsed,
				LogLevel.Critical);
		}

		// Attempt to resize the server, failing if it fails
		var action = await client.DropletActions.Resize(DropletId, size.Slug, resizeDisk: false);
		var resizeFailed = !(await WaitForActionToComplete(action, pollingInterval: 5000, -1));
		if (resizeFailed)
		{
			FreeServerAccess();
			stopwatch.Stop();

			return new ServerActionResult(
				false, $"The server ({ServerName}) resizing operation failed.", stopwatch.Elapsed, LogLevel.Critical);
		}

		// Finally attempt to start the server back up, failing if it fails
		var startFailed = !(await StartServerExecuteAsync()).Success;
		if (startFailed)
		{
			FreeServerAccess();
			stopwatch.Stop();

			return new ServerActionResult(false,
				$"The server ({ServerName}) could not be started again after resizing.",
				stopwatch.Elapsed,
				LogLevel.Critical);
		}

		CurrentSize = (await client.Droplets.Get(DropletId)).Size.ToServerSize();

		FreeServerAccess();
		stopwatch.Stop();

		return new ServerActionResult(
			true, $"The server ({ServerName}) was successfully resized.", stopwatch.Elapsed, LogLevel.Information);
	}

	private async Task<ServerActionResult> HibernateServerExecuteAsync()
	{
		if (CurrentSize == HibernateSize)
		{
			return new ServerActionResult(
				true, $"The server ({ServerName}) is already in hibernation.", TimeSpan.Zero, LogLevel.Information);
		}

		var result = await ScaleServerExecuteAsync(HibernateSize);

		if (result.Success)
		{
			return new ServerActionResult(
				true, $"The server ({ServerName}) was successfully put into hibernation.", result.elapsedTime, LogLevel.Information);
		}
		else
		{
			return new ServerActionResult(
				false, $"The server ({ServerName}) hibernation operation failed.", result.elapsedTime, LogLevel.Critical);
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

	private async Task<Droplet> GetDroplet()
	{
		return await client.Droplets.Get(DropletId);
	}

	private DropletActionStatus Status(string status)
	{
		var dropletActionStatus = DropletActionStatus.FromName(status);
		return DropletActionStatus.FromName(status);
	}

	private void BlockServerAccess()
	{
		IsBusy = true;
	}

	private void FreeServerAccess()
	{
		IsBusy = false;
	}
}
