using Discord;
using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule
{
	[RequireOwner(Group = "ServerManagerPermission")]
	[RequireRole("Game Server Manager", Group = "ServerManagerPermission")]
	[SlashCommand("scale", "Scales server to a specified size.")]
	public async Task ServerScaleAsync()
	{
		await DeferAsync(ephemeral: true);

		var sizeOptions = new List<SelectMenuOptionBuilder>(GameServer.AllowedSizes.Count());

		foreach (var size in GameServer.AllowedSizes)
		{
			var description =
				$"vCpus: {size.Vcpus} | RAM: {size.Memory} | Monthly: ${size.PriceMonthly} | Hourly: ${size.PriceHourly}";

			var sizeOption = new SelectMenuOptionBuilder()
				.WithLabel(size.Slug)
				.WithValue(size.Slug)
				.WithDescription(description);

			sizeOptions.Add(sizeOption);
		}

		var slugSelectMenu = new SelectMenuBuilder()
			.WithCustomId("server-scale-select")
			.WithOptions(sizeOptions);

		var slugSelectComponent = new ComponentBuilder()
			.WithSelectMenu(slugSelectMenu)
			.Build();

		await FollowupAsync("Please select a size to scale to.", ephemeral: false, components: slugSelectComponent);
	}

	[ComponentInteraction("server-scale-select", ignoreGroupNames: true)]
	public async Task ServerScaleSelectAsync(string[] selectedSlugs)
	{
		await DeferAsync(ephemeral: true);

		var selectedSlug = selectedSlugs.First();

		var size = await GameServer.GetServerSizeAsync(selectedSlug);

		var initialMessage = await ReplyAsync(
			$"Attempting to scale the server __**{GameServer.ServerName}**__ to size {size!.Slug}.");

		MessagesToDelete.Add(initialMessage);
		var serverActionResult = await GameServer.ScaleServerAsync(size);

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
