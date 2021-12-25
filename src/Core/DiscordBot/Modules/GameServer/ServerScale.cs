using Discord;
using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule
{
	[SlashCommand("server-scale", "Scales server to a specified slug.")]
	public async Task ServerScaleAsync()
	{
		await DeferAsync(ephemeral: true);

		var sizeSlugOptions = new List<SelectMenuOptionBuilder>(Server.AllowedSlugSizes.Count());

		foreach (var size in Server.AllowedSlugSizes)
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
			.WithCustomId("server-scale-select")
			.WithOptions(sizeSlugOptions);

		var slugSelectComponent = new ComponentBuilder()
			.WithSelectMenu(slugSelectMenu)
			.Build();

		await FollowupAsync("Please select a slug size to scale to.", ephemeral: false, components: slugSelectComponent);
	}

	[ComponentInteraction("server-scale-select")]
	public async Task ServerScaleSelectAsync(string[] selectedSlugs)
	{
		var selectedSlug = selectedSlugs.First();

		var size = Server.GetSlugSize(selectedSlug);

		var initialMessage = await ReplyAsync(
			$"Attempting to scale the server __**{Server.DropletName}**__ to slug {size.Slug}.");

		await DeferAsync(ephemeral: true);

		MessagesToDelete.Add(initialMessage);
		var serverActionResult = await Server.ResizeDroplet(size.Slug);

		await Context.Interaction.ModifyOriginalResponseAsync(message =>
		{
			message.Content = $"Selected {selectedSlug}";
			message.Components = new ComponentBuilder().Build();
		});

		if (serverActionResult.Success)
		{
			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

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
			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"❌\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}", ephemeral: false);
		}
	}
}
