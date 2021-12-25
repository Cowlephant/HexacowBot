using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule
{
	[SlashCommand("balance", "Shows the month to date balance for the server account.")]
	public async Task ServerBalanceAsync()
	{
		await DeferAsync(ephemeral: true);
		var balance = $"${await GameServer.GetMonthToDateBalanceAsync()}";
		var response = $"```Monthly Balance\t {balance}```";
		await FollowupAsync(response, ephemeral: true);
	}
}
