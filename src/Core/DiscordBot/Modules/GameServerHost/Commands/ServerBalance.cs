using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServerHost;

public sealed partial class GameServerModule
{
	[SlashCommand("balance", "Shows the month to date balance for the server account.")]
	public async Task ServerBalanceAsync()
	{
		await DeferAsync(ephemeral: true);
		var balance = $"${await GameServerHost.GetMonthToDateBalanceAsync()}";
		var response = $"```Monthly Balance\t {balance}```";
		await FollowupAsync(response, ephemeral: true);
	}
}
