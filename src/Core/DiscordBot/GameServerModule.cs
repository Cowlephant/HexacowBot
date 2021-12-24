﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using HexacowBot.Core.GameServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace HexacowBot.Core.DiscordBot;

public sealed class GameServerModule : InteractionModuleBase
{
	private const string ownerError = "During development this bot is only usable by the creator.";
	private const string retryPrompt = "Would you like to retry?";

	private readonly DigitalOceanService server;
	private readonly IConfiguration configuration;
	private readonly ILogger logger;

	private List<IUserMessage> messagesToDelete;

	public GameServerModule(
		DigitalOceanService digitalOceanService,
		IConfiguration configuration,
		ILogger<GameServerModule> logger)
	{
		server = digitalOceanService;
		this.configuration = configuration;
		this.logger = logger;

		messagesToDelete = new List<IUserMessage>();
	}

	[SlashCommand("server-balance", "Shows the Month to Date balance for the account.")]
	public async Task ServerBalanceAsync()
	{
		await DeferAsync(ephemeral: true);
		var response = $"```Monthly Balance\t {await server.GetMonthToDateBalance()}```";
		await FollowupAsync(response, ephemeral: true);
	}

	[SlashCommand("server-sizes", "Lists allowed droplet size slugs.")]
	public async Task ServerSizesAsync()
	{
		await DeferAsync(ephemeral: true);

		var allowedSizes = server.AllowedSlugSizes;
		var hibernateSize = server.HibernateSize;

		var response = new StringBuilder();
		response.AppendLine("__Allowed server size slugs__");
		response.AppendLine("```");
		foreach (var size in allowedSizes)
		{
			var slug = size.Slug;
			var active = size.Slug == server.CurrentSize.Slug ? "✅" : string.Empty;
			var hibernation = size == hibernateSize ? "💤" : string.Empty;
			var vCpus = $"vCpus: {size.Vcpus}";
			var ram = $"RAM: {(size.Memory / 1024)}GB";
			var priceMonthly = $"Monthly: ${size.PriceMonthly}";

			response.AppendLine($"{slug}\t{active}{hibernation}");
			response.AppendLine($"{vCpus}\t{ram}\t{priceMonthly}");
			response.AppendLine();
		}
		response.AppendLine("```");

		await FollowupAsync(response.ToString(), ephemeral: true);
	}

	[SlashCommand("server-start", "Boot up the server.")]
	[ComponentInteraction("server-start-retry")]
	public async Task ServerStartAsync()
	{
		await DeferAsync();

		await RetryClearComponentInteraction(Context.Interaction);

		var initialMessage = await ReplyAsync($"Attempting to boot up the server __**{server.DropletName}**__.");
		messagesToDelete.Add(initialMessage);
		var serverActionResult = await server.StartDroplet();

		if (serverActionResult.Success)
		{
			logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"✅\t⬆️🖥\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
		}
		else
		{
			var retryButtonComponent = new ComponentBuilder()
				.WithButton("Retry", "server-start-retry", ButtonStyle.Primary)
				.Build();

			logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await ReplyAsync($"❌\t⬆️🖥\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
			await FollowupAsync(retryPrompt, ephemeral: true, components: retryButtonComponent);
		}
	}

	[SlashCommand("server-stop", "Shuts down the server safely.")]
	[ComponentInteraction("server-stop-retry")]
	public async Task ServerStopAsync()
	{
		await DeferAsync();

		await RetryClearComponentInteraction(Context.Interaction);

		var initialMessage = await ReplyAsync($"Attempting to shut down the server __**{server.DropletName}**__.");
		messagesToDelete.Add(initialMessage);
		var serverActionResult = await server.StopDroplet();

		if (serverActionResult.Success)
		{
			logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"✅\t⬇️🖥\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
		}
		else
		{
			var retryButtonComponent = new ComponentBuilder()
				.WithButton("Retry", "server-stop-retry", ButtonStyle.Primary)
				.WithButton("Abort", "server-abort", ButtonStyle.Danger)
				.Build();

			logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await ReplyAsync($"❌\t⬇️🖥\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
			await FollowupAsync(retryPrompt, ephemeral: true, components: retryButtonComponent);
		}
	}

	[SlashCommand("server-restart", "Restarts the server safely.")]
	[ComponentInteraction("server-restart-retry")]
	public async Task ServerRestartAsync()
	{
		await DeferAsync();

		await RetryClearComponentInteraction(Context.Interaction);

		var initialMessage = await ReplyAsync($"Attempting to restart the server __**{server.DropletName}**__.");
		messagesToDelete.Add(initialMessage);
		var serverActionResult = await server.RestartDroplet();

		if (serverActionResult.Success)
		{
			logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"✅\t🔄🖥\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
		}
		else
		{
			var retryButtonComponent = new ComponentBuilder()
				.WithButton("Retry", "server-restart-retry", ButtonStyle.Primary)
				.WithButton("Abort", "server-abort", ButtonStyle.Danger)
				.Build();

			logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await ReplyAsync($"❌\t🔄🖥\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
			await FollowupAsync(retryPrompt, ephemeral: true, components: retryButtonComponent);
		}
	}

	[SlashCommand("server-resize", "Resizes server to a specified slug.")]
	public async Task ResizeServerAsync()
	{
		await DeferAsync(ephemeral: true);

		var sizeSlugOptions = new List<SelectMenuOptionBuilder>(server.AllowedSlugSizes.Count());

		foreach (var size in server.AllowedSlugSizes)
		{
			var description =
				$"vCpus: {size.Vcpus} | RAM: {size.Memory} | Monthly: ${size.PriceMonthly} | Hourly: ${size.PriceHourly}";

			var slugOption = new SelectMenuOptionBuilder()
				.WithLabel(size.Slug)
				.WithValue(size.Slug)
				.WithDescription(description);

			sizeSlugOptions.Add(slugOption);
		}

		var slugSelectMenu = new SelectMenuBuilder()
			.WithCustomId("server-resize-select")
			.WithOptions(sizeSlugOptions);

		var slugSelectComponent = new ComponentBuilder()
			.WithSelectMenu(slugSelectMenu)
			.Build();

		await FollowupAsync("Please select a slug size to resize to.", ephemeral: false, components: slugSelectComponent);
	}

	[ComponentInteraction("server-resize-select")]
	public async Task ResizeServerSelectAsync(string[] selectedSlugs)
	{
		var selectedSlug = selectedSlugs.First();

		var size = server.GetSlugSize(selectedSlug);

		var initialMessage = await ReplyAsync(
			$"Attempting to resize the server __**{server.DropletName}**__ to slug {size.Slug}.");

		await DeferAsync(ephemeral: true);

		messagesToDelete.Add(initialMessage);
		var serverActionResult = await server.ResizeDroplet(size.Slug);

		await Context.Interaction.ModifyOriginalResponseAsync(message =>
		{
			message.Content = $"Selected {selectedSlug}";
			message.Components = new ComponentBuilder().Build();
		});

		if (serverActionResult.Success)
		{
			logger.Log(serverActionResult.Severity, serverActionResult.Message);

			var response = new StringBuilder();
			response.AppendLine($"✅\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
			response.AppendLine("```");
			response.AppendLine($"{"vCpus".PadRight(15, ' ')} {size.Vcpus}");
			response.AppendLine($"{"Memory".PadRight(15, ' ')} {(size.Memory / 1024)}GB");
			response.AppendLine($"{"Price Hourly".PadRight(15, ' ')} ${size.PriceHourly}");
			response.AppendLine($"{"Price Monthly".PadRight(15, ' ')} ${size.PriceMonthly}");
			response.AppendLine("```");

			await FollowupAsync(response.ToString(), ephemeral: false);
		}
		else
		{
			logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"❌\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}", ephemeral: false);
		}
	}

	[ComponentInteraction("server-abort")]
	public async Task ServerAbortAsync()
	{
		var component = (Context.Interaction as SocketMessageComponent)!;

		messagesToDelete.Add(await component.GetOriginalResponseAsync());
		await component.UpdateAsync(message =>
		{
			message.Content = "Aborted.";
			message.Components = new ComponentBuilder().Build();
		});
	}

	[RequireOwner]
	public async Task ServerPowerCycleAsync()
	{
		_ = server.PowerCycleDroplet(async () => { await ReplyAsync("Server is restarted."); });
		await ReplyAsync("Power cycling the server. I will let you know when it's finished.");
	}

	private async Task RetryClearComponentInteraction(IDiscordInteraction interaction)
	{
		if (interaction is SocketMessageComponent)
		{
			// We should be attempting to retry, so we'll remove the original interaction
			var component = (interaction as SocketMessageComponent)!;

			var originalResponse = await component.GetOriginalResponseAsync();
			if (originalResponse is not null)
			{
				var disabledButton = new ComponentBuilder()
					.Build();

				await originalResponse.ModifyAsync(message =>
				{
					message.Content = "Retrying...";
					message.Components = disabledButton;
				});
			}
		}
	}

	private string GetElapsedFriendly(TimeSpan timespan)
	{
		if (timespan == TimeSpan.Zero)
		{
			return string.Empty;
		}

		return $"({Math.Round(timespan.TotalSeconds, 0)} sec)";
	}

	public override async void AfterExecute(ICommandInfo command)
	{
		foreach (var message in messagesToDelete)
		{
			await message.DeleteAsync();
		}
	}
}