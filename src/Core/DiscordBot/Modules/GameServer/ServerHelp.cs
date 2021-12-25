using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule
{
	[SlashCommand("help", "Shows general help information for the server commands.")]
	public async Task ServerHelpAsync()
	{
		var response = new StringBuilder();
		response.AppendLine("__Server Help List__");
		response.AppendLine("Commands begin with **/server**, e.g. /server balance");

		response.AppendLine("**balance** - Shows the monthly accrued balance for the entire server account. " +
			"This may or may not include other resources besides just the server.");

		response.AppendLine("**sizes** - Shows all of the allowed server sizes the server can be scaled to, excluding " +
			"all others deemed inappropriate for the server's needs. This will include information about the vCpus, " +
			"RAM, monthly fee as well as indicating which size the server is currently scaled to (✅) and which size " +
			"the server will scale to when hibernated (💤).");

		response.AppendLine("**start** - Attempts to start (boot up) the server up if it is in the stopped state.");

		response.AppendLine("**stop** - Attempts to stop (shut down) the server if it is in the started state.");

		response.AppendLine("**restart** - Attempts to safely restart the server by first shutting it down and then " +
			"booting it back up. If it cannot gracefully shut down the server after a timeout, it will force power off.");

		response.AppendLine("**powercycle** - Attempts to **unsafely** restart the server by power cycling. This may " +
			"result in data loss or corruption and should only be used if the server seems to be hanging.");

		response.AppendLine("**scale** - Attempts to scale the server to a specified size, safely shutting it down " +
			"first and then starting it back up when the scaling is complete.");

		await RespondAsync(response.ToString(), ephemeral: true);
	}
}
