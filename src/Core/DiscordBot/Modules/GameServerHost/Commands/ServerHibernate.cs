using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServerHost;

public sealed partial class GameServerModule
{
	[RequireOwner(Group = "ServerManagerPermission")]
	[RequireRole("Game Server Manager", Group = "ServerManagerPermission")]
	[SlashCommand("hibernate", "Scales the server to the smallest possible size for cheapest costs when idle.")]
	public async Task ServerHibernateAsync()
	{
		await DeferAsync(ephemeral: false);

		await SetCustomStatus("Hibernation Preparations");

		await RetryClearComponentInteraction(Context.Interaction);

		var initialMessage = await ReplyAsync($"Preparing server __**{GameServerHost.ServerName}**__ for hibernation.");
		MessagesToDelete.Add(initialMessage);
		var serverActionResult = await GameServerHost.HibernateServerAsync();

		await GameServerStatusHelper.SetServerStatus(Context.Client, GameServerHost);

		if (serverActionResult.Success)
		{
			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"✅\t💤\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
		}
		else
		{
			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"❌\t💤\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
		}
	}
}
