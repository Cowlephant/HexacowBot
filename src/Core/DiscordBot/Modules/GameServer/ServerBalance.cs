using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule
{
	[SlashCommand("server-balance", "Shows the Month to Date balance for the account.")]
	public async Task ServerBalanceAsync()
	{
		await DeferAsync(ephemeral: true);
		var response = $"```Monthly Balance\t {await Server.GetMonthToDateBalance()}```";
		await FollowupAsync(response, ephemeral: true);
	}
}
