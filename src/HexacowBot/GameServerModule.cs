using Discord.Commands;
using Discord.Interactions;
using System.Text;

namespace HexacowBot
{
	public sealed class GameServerModule : ModuleBase<SocketCommandContext>
	{
		private readonly DigitalOceanService server;
		private readonly IConfiguration configuration;

		public GameServerModule(DigitalOceanService digitalOceanService, IConfiguration configuration)
		{
			this.server = digitalOceanService;
			this.configuration = configuration;
		}

		[Command("server-resize")]
		public async Task ResizeServerAsync(string sizeSlug)
		{
			// Let user know if it's not an acceptable slug
			var allowedSizes = configuration.GetSection("DigitalOcean:AllowedSlugs").Get<string[]>();
			if (!allowedSizes.Contains(sizeSlug))
			{
				await ReplyAsync("Provided slug was not recognized or allowed.");
				return;
			}

			_ = Task.Run(() => server.ResizeDroplet(sizeSlug,
				async () =>
				{
					var response = new StringBuilder();
					response.AppendLine("__Game Server has finished resizing__");
					response.AppendLine("```");
					response.AppendLine($"{"vCpus".PadRight(15, ' ')} {server.currentSize.Vcpus}");
					response.AppendLine($"{"Memory".PadRight(15, ' ')} {(server.currentSize.Memory / 1024)}GB");
					response.AppendLine($"{"Price Hourly".PadRight(15, ' ')} ${server.currentSize.PriceHourly}");
					response.AppendLine($"{"Price Monthly".PadRight(15, ' ')} ${server.currentSize.PriceMonthly}");
					response.AppendLine($"{"Monthly Balance".PadRight(15, ' ')} ${server.currentBalance.MonthToDateBalance}");
					response.AppendLine("```");

					await ReplyAsync(response.ToString());
				}));

			await ReplyAsync("Game Server resizing. I'll let you know when it's finished.");
		}

		[Command("server-sizes")]
		public async Task ServerSizesAsync()
		{
			var allowedSizes = configuration.GetSection("DigitalOcean:AllowedSlugs").Get<string[]>();

			var response = new StringBuilder();
			response.AppendLine("__Allowed server size slugs__");
			response.AppendLine("```");
			foreach (var size in allowedSizes)
			{
				var monthlyPrice = server.GetSlugMonthlyCost(size);
				var formatSlug = size.PadRight(15, ' ');
				var formatPrice = $"{monthlyPrice.PadRight(5, ' ')} - monthly";
				response.AppendLine($"{formatSlug}\t{formatPrice}");
			}
			response.AppendLine("```");

			await ReplyAsync(response.ToString());
		}

		[Command("server-balance")]
		public async Task ServerBalanceAsync()
		{
			var response = $"```Monthly Balance\t ${server.currentBalance.MonthToDateBalance}```";
			await ReplyAsync(response);
		}
	}
}