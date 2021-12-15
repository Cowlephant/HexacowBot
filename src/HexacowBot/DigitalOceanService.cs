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
	public Balance currentBalance { get; private set; } = null!;
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
		currentBalance = await client.BalanceClient.Get();
		slugPrices = (await client.Sizes.GetAll())
			.Select(s => (s.Slug, $"${s.PriceMonthly}"))
			.ToHashSet<(string, string)>();
		currentSize = droplet.Size;
	}

	public async Task ResizeDroplet(string sizeSlug, System.Action callback)
	{
		// TODO: Check if there are running servers first and abort and notify if so
		// Attempt to shut down droplet first
		await ShutdownDroplet(dropletId);

		var action = await client.DropletActions.Resize(dropletId, sizeSlug, resizeDisk: false);
		while (action.Status != "completed")
		{
			action = await client.Actions.Get(action.Id);
			Thread.Sleep(actionPollInterval);
		}

		await StartDroplet(dropletId);

		droplet = await client.Droplets.Get(dropletId);
		currentSize = droplet.Size;
		currentBalance = await client.BalanceClient.Get();

		callback();
	}

	public string GetSlugMonthlyCost(string sizeSlug)
	{
		return slugPrices.First(s => s.Item1 == sizeSlug).Item2;
	}

	private async Task ShutdownDroplet(long dropletId)
	{
		// Shut down the server and wait until it's completed
		var action = await client.DropletActions.Shutdown(dropletId);
		int timeoutCount = 0;
		while (action.Status != "completed" && timeoutCount < shutdownTimeout)
		{
			action = await client.Actions.Get(action.Id);
			Thread.Sleep(actionPollInterval);
			timeoutCount++;
		}

		// If shutdown wasn't successful after interval, attempt a power off
		while (action.Status != "completed")
		{
			action = await client.DropletActions.PowerOff(dropletId);
			Thread.Sleep(actionPollInterval);
		}
	}

	private async Task StartDroplet(long dropletId)
	{
		// Shut down the server and wait until it's completed
		var action = await client.DropletActions.PowerOn(dropletId);
		while (action.Status != "completed")
		{
			action = await client.Actions.Get(action.Id);
			Thread.Sleep(actionPollInterval);
		}
	}
}
