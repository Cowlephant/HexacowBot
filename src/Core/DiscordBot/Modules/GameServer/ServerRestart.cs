using Discord;
using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule
{
	[RequireOwner]
	[SlashCommand("restart", "Restarts the server safely.")]
	[ComponentInteraction("server-restart-retry", ignoreGroupNames: true)]
	public async Task ServerRestartAsync()
	{
		await DeferAsync();

		await RetryClearComponentInteraction(Context.Interaction);

		var initialMessage = await ReplyAsync($"Attempting to restart the server __**{GameServer.ServerName}**__.");
		MessagesToDelete.Add(initialMessage);
		var serverActionResult = await GameServer.RestartServerAsync();

		if (serverActionResult.Success)
		{
			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"✅\t🔄🖥\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
		}
		else
		{
			var retryButtonComponent = new ComponentBuilder()
				.WithButton("Retry", "server-restart-retry", ButtonStyle.Primary)
				.WithButton("Abort", "server-abort", ButtonStyle.Danger)
				.Build();

			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await ReplyAsync($"❌\t🔄🖥\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
			await FollowupAsync(RetryPrompt, ephemeral: true, components: retryButtonComponent);
		}
	}
}
