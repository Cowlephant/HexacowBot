using Discord;
using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule
{
	[RequireOwner]
	[SlashCommand("start", "Boot up the server.")]
	[ComponentInteraction("server-start-retry", ignoreGroupNames: true)]
	public async Task ServerStartAsync()
	{
		await DeferAsync();

		await SetCustomStatus("Starting");

		await RetryClearComponentInteraction(Context.Interaction);

		var initialMessage = await ReplyAsync($"Attempting to boot up the server __**{GameServer.ServerName}**__.");
		MessagesToDelete.Add(initialMessage);
		var serverActionResult = await GameServer.StartServerAsync();

		if (serverActionResult.Success)
		{
			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"✅\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
		}
		else
		{
			var retryButtonComponent = new ComponentBuilder()
				.WithButton("Retry", "server-start-retry", ButtonStyle.Primary)
				.Build();

			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await ReplyAsync($"❌\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
			await FollowupAsync(RetryPrompt, ephemeral: true, components: retryButtonComponent);
		}

		await ClearCustomStatus();
	}
}
