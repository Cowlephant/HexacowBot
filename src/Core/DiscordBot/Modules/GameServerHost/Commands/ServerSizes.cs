using Discord.Interactions;

namespace HexacowBot.Core.DiscordBot.Modules.GameServerHost;

public sealed partial class GameServerHostModule
{
	[SlashCommand("sizes", "Lists allowed droplet sizes.")]
	public async Task ServerSizesAsync()
	{
		await DeferAsync(ephemeral: true);

		var allowedSizes = GameServerHost.AllowedSizes;
		var hibernateSize = GameServerHost.HibernateSize;

		var response = new StringBuilder();
		response.AppendLine("__Allowed server sizes__");
		response.AppendLine("```");
		foreach (var size in allowedSizes)
		{
			var slug = size.Slug;
			var active = size.Slug == GameServerHost.CurrentSize.Slug ? "✅" : string.Empty;
			var hibernation = size == hibernateSize ? "💤" : string.Empty;
			var vCpus = $"vCpus: {size.Vcpus}";
			var ram = $"RAM: {(size.Memory / 1024)}GB";
			var priceMonthly = $"Monthly: ${size.PriceMonthly}";

			response.AppendLine($"{slug}\t{active}{hibernation}");
			response.AppendLine($"{vCpus}\t{ram}\t{priceMonthly}");
			response.AppendLine();
		}
		response.AppendLine("```");

		await FollowupAsync(response.ToString(), ephemeral: true);
	}
}
