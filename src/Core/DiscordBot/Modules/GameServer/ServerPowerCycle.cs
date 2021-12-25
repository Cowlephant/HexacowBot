using Discord;
using Discord.Interactions;
using HexacowBot.Core.GameServer;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule
{
	[RequireOwner]
	[SlashCommand("powercycle", "Boot up the server.")]
	public async Task ServerPowerCycleAsync()
	{
		await DeferAsync();

		await SetCustomStatus("Power Cycling");

		var initialMessage = await ReplyAsync($"Attempting to power cycle the server __**{GameServer.ServerName}**__.");
		MessagesToDelete.Add(initialMessage);
		var serverActionResult = await GameServer.PowerCycleServerAsync();

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

		await ClearCustomStatus();
	}
}
