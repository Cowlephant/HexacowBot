using DigitalOcean.API.Models.Responses;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using System.Text;

namespace HexacowBot;

public sealed class GameServerModule : InteractionModuleBase
{
	private const string ownerError = "During development this bot is only usable by the creator.";

	private readonly DigitalOceanService server;
	private readonly IConfiguration configuration;

	private List<IUserMessage> messagesToDelete;

	public GameServerModule(DigitalOceanService digitalOceanService, IConfiguration configuration)
	{
		server = digitalOceanService;
		this.configuration = configuration;

		messagesToDelete = new List<IUserMessage>();
	}

	[SlashCommand("server-balance", "Shows the Month to Date balance for the account.")]
	public async Task ServerBalanceAsync()
	{
		var response = $"```Monthly Balance\t ${await server.GetMonthToDateBalance()}```";
		await RespondAsync(response, ephemeral: true);
	}

	[SlashCommand("server-sizes", "Lists allowed droplet size slugs.")]
	public async Task ServerSizesAsync()
	{
		var allowedSizes = configuration.GetSection("DigitalOcean:AllowedSlugs").Get<string[]>();
		var hibernateSize = configuration["DigitalOcean:HibernateSlug"];

		var response = new StringBuilder();
		response.AppendLine("__Allowed server size slugs__");
		response.AppendLine("```");
		foreach (var sizeSlug in allowedSizes)
		{
			var size = server.GetSlugSize(sizeSlug);

			var formatSlug = sizeSlug.PadRight(15, ' ');
			var formatPrice = $"{size.PriceMonthly.ToString().PadRight(5, ' ')} - monthly";
			response.AppendLine($"{formatSlug}\t{formatPrice}");
		}
		response.AppendLine("```");

		await RespondAsync(response.ToString(), ephemeral: true);
	}

	//[RequireOwner]
	[SlashCommand("server-start", "Boot up the server.")]
	[ComponentInteraction("server-start-retry")]
	public async Task ServerStartAsync()
	{
		await DeferAsync();

		await RetryClearComponentInteraction(Context.Interaction);

		var initialMessage = await ReplyAsync($"Attempting to boot up the server __**{server.DropletName}**__.");
		messagesToDelete.Add(initialMessage);
		var success = await server.StartDroplet();

		if (success)
		{
			await FollowupAsync($"✅\t⬆️🖥 Server __**{server.DropletName}**__ successfully booted up.");
		}
		else
		{
			var retryButtonComponent = new ComponentBuilder()
				.WithButton("Retry", "server-start-retry", ButtonStyle.Primary)
				.Build();

			await ReplyAsync($"❌\t⬆️🖥 Something went wrong trying to boot up the server __**{server.DropletName}**__.");
			await FollowupAsync("Would you like to retry?", ephemeral: true, component: retryButtonComponent);
		}
	}

	//[RequireOwner]
	[SlashCommand("server-stop", "Shuts down the server safely.")]
	[ComponentInteraction("server-stop-retry")]
	public async Task ServerStopAsync()
	{
		await DeferAsync();

		await RetryClearComponentInteraction(Context.Interaction);

		var initialMessage = await ReplyAsync($"Attempting to shut down the server __**{server.DropletName}**__.");
		messagesToDelete.Add(initialMessage);
		var success = await server.StopDroplet();

		if (success)
		{
			await FollowupAsync($"✅\t⬇️🖥 Server __**{server.DropletName}**__ successfully shut down.");
		}
		else
		{
			var retryButtonComponent = new ComponentBuilder()
				.WithButton("Retry", "server-stop-retry", ButtonStyle.Primary)
				.WithButton("Abort", "server-abort", ButtonStyle.Danger)
				.Build();

			await ReplyAsync($"❌\t⬇️🖥 Something went wrong trying to shut down the server __**{server.DropletName}**__.");
			await FollowupAsync("Would you like to retry?", ephemeral: true, component: retryButtonComponent);
		}
	}

	//[RequireOwner]
	[SlashCommand("server-restart", "Restarts the server safely.")]
	[ComponentInteraction("server-restart-retry")]
	public async Task ServerRestartAsync()
	{
		await DeferAsync();

		await RetryClearComponentInteraction(Context.Interaction);

		var initialMessage = await ReplyAsync($"Attempting to restart the server __**{server.DropletName}**__.");
		messagesToDelete.Add(initialMessage);
		var success = await server.RestartDroplet();

		if (success)
		{
			await FollowupAsync($"✅\t🔄🖥 Server __**{server.DropletName}**__ successfully restarted.");
		}
		else
		{
			var retryButtonComponent = new ComponentBuilder()
				.WithButton("Retry", "server-restart-retry", ButtonStyle.Primary)
				.WithButton("Abort", "server-abort", ButtonStyle.Danger)
				.Build();

			await ReplyAsync($"❌\t🔄🖥 Something went wrong trying to restart the server __**{server.DropletName}**__.");
			await FollowupAsync("Would you like to retry?", ephemeral: true, component: retryButtonComponent);
		}
	}

	[SlashCommand("server-resize", "Resizes server to a specified slug.")]
	public async Task ResizeServerAsync()
	{
		await DeferAsync(ephemeral: true);

		var allowedSizes = configuration.GetSection("DigitalOcean:AllowedSlugs").Get<string[]>();

		var sizeSlugOptions = new List<SelectMenuOptionBuilder>(allowedSizes.Length);

		foreach (var sizeSlug in allowedSizes)
		{
			var size = server.GetSlugSize(sizeSlug);
			var description =
				$"vCpus: {size.Vcpus} | RAM: {size.Memory} | Monthly: ${size.PriceMonthly} | Hourly: ${size.PriceHourly}";

			var slugOption = new SelectMenuOptionBuilder()
				.WithLabel(sizeSlug)
				.WithValue(sizeSlug)
				.WithDescription(description);

			sizeSlugOptions.Add(slugOption);
		}

		var slugSelectMenu = new SelectMenuBuilder()
			.WithCustomId("server-resize-select")
			.WithOptions(sizeSlugOptions);

		var slugSelectComponent = new ComponentBuilder()
			.WithSelectMenu(slugSelectMenu)
			.Build();

		await FollowupAsync("Please select a slug size to resize to.", ephemeral: false, component: slugSelectComponent);
	}

	//[RequireOwner]
	[ComponentInteraction("server-resize-select")]
	public async Task ResizeServerSelectAsync(string[] selectedSlugs)
	{
		var selectedSlug = selectedSlugs.First();

		var size = server.GetSlugSize(selectedSlug);

		var initialMessage = await ReplyAsync(
			$"Attempting to resize the server __**{server.DropletName}**__ to slug {size.Slug}.");

		await DeferAsync(ephemeral: true);

		messagesToDelete.Add(initialMessage);
		var success = await server.ResizeDroplet(size.Slug);

		if (success)
		{
			var response = new StringBuilder();
			response.AppendLine($"✅\tServer __**{server.DropletName}**__ has finished resizing to slug {size.Slug}.");
			response.AppendLine("```");
			response.AppendLine($"{"vCpus".PadRight(15, ' ')} {size.Vcpus}");
			response.AppendLine($"{"Memory".PadRight(15, ' ')} {(size.Memory / 1024)}GB");
			response.AppendLine($"{"Price Hourly".PadRight(15, ' ')} ${size.PriceHourly}");
			response.AppendLine($"{"Price Monthly".PadRight(15, ' ')} ${size.PriceMonthly}");
			response.AppendLine("```");

			await FollowupAsync(response.ToString());
		}
		else
		{
			await FollowupAsync($"❌\tSomething went wrong trying to resize the server __**{server.DropletName}**__.");
		}

		await DeleteOriginalResponseAsync();
	}

	[ComponentInteraction("server-abort")]
	public async Task ServerAbortAsync()
	{
		var component = (Context.Interaction as SocketMessageComponent)!;

		messagesToDelete.Add(await component.GetOriginalResponseAsync());
		await component.UpdateAsync(message =>
		{
			var clearComponents = new ComponentBuilder()
				.Build();

			message.Content = "Aborted.";
			message.Components = clearComponents;
		});
	}

	//[RequireOwner]
	//[Command("server-powercycle")]
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
			//await component.DeferAsync();

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

	public override async void AfterExecute(ICommandInfo command)
	{
		foreach (var message in messagesToDelete)
		{
			await message.DeleteAsync();
		}
	}
}
