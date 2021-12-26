using Discord;
using Discord.WebSocket;
using HexacowBot.Core.GameServerHost;

namespace HexacowBot.Core.DiscordBot;

public static class GameServerStatusHelper
{
	public static async Task SetServerStatus(IDiscordClient client, IGameServerHost gameServerHost)
	{
		var isStarted = await gameServerHost.CheckIsStarted();
		var isHibernating = await gameServerHost.CheckIsHibernating();

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
