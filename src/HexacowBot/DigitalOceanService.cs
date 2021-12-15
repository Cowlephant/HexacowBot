using DigitalOcean.API;
using DigitalOcean.API.Models.Responses;

namespace HexacowBot;

public sealed class DigitalOceanService
{
	private const int shutdownTimeout = 90;
	private const int actionPollInterval = 1000;

	private readonly DigitalOceanClient client;
	private readonly IConfiguration configuration;

	public long dropletId { get; private set; }
	public Droplet droplet { get; private set; } = null!;
	public Size currentSize { get; private set; } = null!;
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
		dropletId = long.Parse(configuration["DigitalOcean:DropletId"]);

		droplet = await client.Droplets.Get(dropletId);
		slugPrices = (await client.Sizes.GetAll())
			.Select(s => (s.Slug, $"${s.PriceMonthly}"))
			.ToHashSet<(string, string)>();
		currentSize = droplet.Size;
	}

	public async Task ResizeDroplet(string sizeSlug, System.Action? callback)
	{
		// TODO: Check if there are running servers first and abort and notify if so
		// Attempt to shut down droplet first
		await StopDroplet();

		var action = await client.DropletActions.Resize(dropletId, sizeSlug, resizeDisk: false);
		await WaitForActionToComplete(action, pollingInterval: 5000, -1); 

		await StartDroplet();

		droplet = await client.Droplets.Get(dropletId);
		currentSize = droplet.Size;

		callback!();
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

	public async Task StopDroplet(System.Action? callback = null)
	{
		// Shut down the server and wait until it's completed
		var action = await client.DropletActions.Shutdown(dropletId);
		var success = await WaitForActionToComplete(action);

		// If shutdown wasn't successful after interval, attempt a power off
		if (!success)
		{
			action = await client.DropletActions.PowerOff(dropletId);
			await WaitForActionToComplete(action, actionPollInterval);
		}

		callback!();
	}

	public async Task StartDroplet(System.Action? callback = null)
	{
		// Start the server and wait until it's completed
		var action = await client.DropletActions.PowerOn(dropletId);
		await WaitForActionToComplete(action);

		callback!();
	}

	public async Task RestartDroplet(System.Action? callback = null)
	{
		var action = await client.DropletActions.Reboot(dropletId);
		await WaitForActionToComplete(action);

		callback!();
	}

	public async Task PowerCycleDroplet(System.Action? callback = null)
	{
		var action = await client.DropletActions.PowerCycle(dropletId);
		await WaitForActionToComplete(action);

		callback!();
	}

	private async Task<bool> WaitForActionToComplete(
		DigitalOcean.API.Models.Responses.Action action,
		int pollingInterval = actionPollInterval,
		int pollingMaxTimes = 90)
	{
		int timeoutCount = 0;
		while (Status(action.Status) != DropletActionStatus.Completed)
		{
			action = await client.Actions.Get(action.Id);

			if (Status(action.Status) == DropletActionStatus.)

			Thread.Sleep(pollingInterval);
			timeoutCount++;

			// If we have a max time of -1, we'll retry indefinitely
			if (pollingMaxTimes != -1 && timeoutCount >= pollingMaxTimes)
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
