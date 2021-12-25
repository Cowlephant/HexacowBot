using Discord;
using Discord.WebSocket;
using HexacowBot.Core.GameServer;

namespace HexacowBot.Core.DiscordBot;

public static class GameServerStatusHelper
{
	public static async Task SetServerStatus(IDiscordClient client, IGameServer gameServer)
	{
		var isStarted = await gameServer.CheckIsStarted();
		var isHibernating = await gameServer.CheckIsHibernating();

		if (isStarted && isHibernating)
		{
			await SetCustomStatus("Hibernating");
		}
		else if (isHibernating && !isStarted)
		{
			await SetCustomStatus("Hibernating Stopped");
		}
		else if (isStarted)
		{
			await SetCustomStatus("Ready");
		}
		else
		{
			await SetCustomStatus("Stopped");
		}

		await Task.CompletedTask;

		async Task SetCustomStatus(string status) {
			await (client as DiscordSocketClient)!.SetGameAsync(status, type: ActivityType.Playing);
		}
	}
}
