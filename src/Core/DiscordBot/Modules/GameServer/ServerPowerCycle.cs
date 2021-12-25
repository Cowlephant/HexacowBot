using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule
{
	[RequireOwner]
	public async Task ServerPowerCycleAsync()
	{
		_ = Server.PowerCycleDroplet(async () => { await ReplyAsync("Server is restarted."); });
		await ReplyAsync("Power cycling the server. I will let you know when it's finished.");
	}
}
