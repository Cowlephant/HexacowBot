using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServerHost;

public sealed partial class GameServerHostModule
{
	[RequireOwner]
	[SlashCommand("powercycle", "Boot up the server.")]
	public async Task ServerPowerCycleAsync()
	{
		await DeferAsync();

		await SetCustomStatus("Power Cycling");

		var initialMessage = await ReplyAsync($"Attempting to power cycle the server __**{GameServerHost.ServerName}**__.");
		MessagesToDelete.Add(initialMessage);
		var serverActionResult = await GameServerHost.PowerCycleServerAsync();

		await GameServerStatusHelper.SetServerStatus(Context.Client, GameServerHost);

		if (serverActionResult.Success)
		{
			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"✅\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
		}
		else
		{
			Logger.Log(serverActionResult.Severity, serverActionResult.Message);

			await FollowupAsync($"❌\t{serverActionResult.Message} {GetElapsedFriendly(serverActionResult.elapsedTime)}");
		}
	}
}
