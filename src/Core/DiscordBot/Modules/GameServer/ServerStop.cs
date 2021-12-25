using Discord;
using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule
{
	[RequireOwner]
	[SlashCommand("stop", "Shuts down the server safely.")]
	[ComponentInteraction("server-stop-retry", ignoreGroupNames: true)]
	public async Task ServerStopAsync()
	{
		await DeferAsync();

		await SetCustomStatus("Stopping");

		await RetryClearComponentInteraction(Context.Interaction);

		var initialMessage = await ReplyAsync($"Attempting to shut down the server __**{GameServer.ServerName}**__.");
		MessagesToDelete.Add(initialMessage);
		var serverActionResult = await GameServer.StopServerAsync();

		if (serverActionResult.Success)
		{
			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await SetCustomStatus("Stopped");

			await FollowupAsync($"✅\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
		}
		else
		{
			var retryButtonComponent = new ComponentBuilder()
				.WithButton("Retry", "server-stop-retry", ButtonStyle.Primary)
				.WithButton("Abort", "server-abort", ButtonStyle.Danger)
				.Build();

			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await ClearCustomStatus();

			await ReplyAsync($"❌\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
			await FollowupAsync(RetryPrompt, ephemeral: true, components: retryButtonComponent);
		}
	}
}
