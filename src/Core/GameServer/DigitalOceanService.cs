﻿using DigitalOcean.API;
using DigitalOcean.API.Models.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HexacowBot.Core.GameServer;

public sealed class DigitalOceanService
{
	private const int shutdownTimeout = 90;
	private const int actionPollInterval = 1000;

	private readonly DigitalOceanClient client;
	private readonly IConfiguration configuration;

	public long DropletId { get; private set; }
	public string DropletName { get; private set; } = "Not Named - Rename me in config";
	public bool IsBusy { get; private set; }

	public Size CurrentSize { get; private set; } = null!;
	public Size HibernateSize { get; private set; } = null!;

	private HashSet<(string, Size)> slugSizes = null!;
	public IEnumerable<(string, Size)> SlugSizes => slugSizes.AsEnumerable();

	private HashSet<Size> allowedSizes = null!;
	public IEnumerable<Size> AllowedSlugSizes => allowedSizes.AsEnumerable();

	private HashSet<(string, string)> sizePrices = null!;
	public IEnumerable<(string, string)> SlugPrices => sizePrices.AsEnumerable();

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

		var allowedSlugs = configuration.GetSection("DigitalOcean:AllowedSlugs").Get<string[]>();
		allowedSizes = allowedSlugs.Select(allowed => slugSizes
			.FirstOrDefault(size => allowed == size.Item1).Item2)
			.ToHashSet();

		CurrentSize = (await client.Droplets.Get(DropletId)).Size;

		var hibernateSlug = configuration["DigitalOcean:HibernateSlug"];
		HibernateSize = slugSizes.First(s => s.Item1 == hibernateSlug).Item2;

		var hibernateSlugNotAllowed = !(allowedSizes.Any(s => s == HibernateSize));
		if (hibernateSlugNotAllowed)
		{
			throw new DigitalOceanException("Hibernate slug is not in configured list of allowed slugs.");
		}
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
		var droplet = await GetDroplet();
		var alreadyStarted = DropletStatus.FromName(droplet.Status) == DropletStatus.Active;
		if (alreadyStarted)
		{
			return new ServerActionResult(true, 
				$"The server ({DropletName}) is already started.", 
				TimeSpan.Zero, 
				LogLevel.Information);
		}

		var stopwatch = new Stopwatch();
		stopwatch.Start();

		var action = await client.DropletActions.PowerOn(DropletId);

		var success = await WaitForActionToComplete(action);

		stopwatch.Stop();

		if (success)
		{
			return new ServerActionResult(
				true, $"The server ({DropletName}) was successfully started.", stopwatch.Elapsed, LogLevel.Information);
		}
		else
		{
			return new ServerActionResult(
				false, $"The server ({DropletName}) start operation failed.", stopwatch.Elapsed, LogLevel.Critical);
		}
	}

	public async Task<ServerActionResult> StopDroplet()
	{
		var droplet = await GetDroplet();
		var alreadyOff = DropletStatus.FromName(droplet.Status) == DropletStatus.Off;
		if (alreadyOff)
		{
			return new ServerActionResult(true,
				$"The server ({DropletName}) is already stopped.",
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

			stopwatch.Stop();

			if (powerOffFailed)
			{
				return new ServerActionResult(
					false, $"The server ({DropletName}) stop operation failed.", stopwatch.Elapsed, LogLevel.Critical);
			}

			return new ServerActionResult(
				true, $"The server ({DropletName}) was stopped ungracefully.", stopwatch.Elapsed, LogLevel.Warning);
		}

		stopwatch.Stop();

		return new ServerActionResult(
			true, $"The server ({DropletName}) was successfully stopped.", stopwatch.Elapsed, LogLevel.Information);
	}

	public async Task<ServerActionResult> RestartDroplet()
	{
		var stopwatch = new Stopwatch();
		stopwatch.Start();

		var action = await client.DropletActions.Reboot(DropletId);
		var success = await WaitForActionToComplete(action);

		stopwatch.Stop();

		if (success)
		{
			return new ServerActionResult(
				true, $"The server ({DropletName}) was successfully restarted.", stopwatch.Elapsed, LogLevel.Information);
		}
		else
		{
			return new ServerActionResult(
				false, $"The server ({DropletName}) reboot operation failed.", stopwatch.Elapsed, LogLevel.Critical);
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

	public async Task<ServerActionResult> ResizeDroplet(string sizeSlug)
	{
		var stopwatch = new Stopwatch();
		stopwatch.Start();

		var isDisallowedSlugSize = !(allowedSizes.Any(s => s.Slug == sizeSlug));
		var currentSize = await GetDropletSize();

		if (isDisallowedSlugSize)
		{
			stopwatch.Stop();

			return new ServerActionResult(false, 
				$"The specified slug size is not a valid size for server ({DropletName})", 
				stopwatch.Elapsed, 
				LogLevel.Warning);
		}
		if (currentSize.Slug == sizeSlug)
		{
			stopwatch.Stop();

			return new ServerActionResult(
				false, $"The server ({DropletName}) is already this size.", stopwatch.Elapsed, LogLevel.Information);
		}

		// TODO: Check if there are running servers first and abort and notify if so
		// Attempt to shut down droplet first, failing if it fails
		var stopFailed = !(await StopDroplet()).Success;
		if (stopFailed)
		{
			stopwatch.Stop();

			return new ServerActionResult(false,
				$"The server ({DropletName}) could not be stopped while attempting to resize.",
				stopwatch.Elapsed,
				LogLevel.Critical);
		}

		// Attempt to resize the server, failing if it fails
		var action = await client.DropletActions.Resize(DropletId, sizeSlug, resizeDisk: false);
		var resizeFailed = !(await WaitForActionToComplete(action, pollingInterval: 5000, -1));
		if (resizeFailed)
		{
			stopwatch.Stop();

			return new ServerActionResult(
				false, $"The server ({DropletName}) resizing operation failed.", stopwatch.Elapsed, LogLevel.Critical);
		}

		// Finally attempt to start the server back up, failing if it fails
		var startFailed = !(await StartDroplet()).Success;
		if (startFailed)
		{
			stopwatch.Stop();

			return new ServerActionResult(false,
				$"The server ({DropletName}) could not be started again after resizing.",
				stopwatch.Elapsed,
				LogLevel.Critical);
		}

		CurrentSize = (await client.Droplets.Get(DropletId)).Size;

		stopwatch.Stop();

		return new ServerActionResult(
			true, $"The server ({DropletName}) was successfully resized.", stopwatch.Elapsed, LogLevel.Information);
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
}