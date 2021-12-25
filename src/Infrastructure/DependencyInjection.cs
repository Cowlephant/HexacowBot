using DigitalOcean.API;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using HexacowBot.Core.GameServer;
using HexacowBot.Infrastructure.DiscordBot;
using HexacowBot.Infrastructure.GameServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton(serviceCollection => new InteractionService(
			serviceCollection.GetService<DiscordSocketClient>(),
			new InteractionServiceConfig
			{
				DefaultRunMode = Discord.Interactions.RunMode.Async,
				ThrowOnError = true,
				LogLevel = Discord.LogSeverity.Verbose
			}));
		services.AddSingleton(new CommandService(
			new CommandServiceConfig
			{
				DefaultRunMode = Discord.Commands.RunMode.Async,
				ThrowOnError = true,
				CaseSensitiveCommands = false,
				LogLevel = Discord.LogSeverity.Verbose
			}));
		services.AddSingleton<CommandHandler>();

		services.AddSingleton<IGameServer, DigitalOceanService>();

		services.AddSingleton(_ =>
		{
			var apiToken = configuration["DigitalOcean:ApiToken"];
			var client = new DigitalOceanClient(apiToken);

			return client;
		});

		services.AddSingleton<Bot>();

		services.AddSingleton<DiscordSocketClient, BotClient>(serviceCollection => new BotClient(
			new DiscordSocketConfig
			{
				UseInteractionSnowflakeDate = false,
				GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers
			},
			configuration,
			serviceCollection.GetRequiredService<ILogger<BotClient>>()));

		return services;
	}

	public static void WarmUpInfrastructure(this IApplicationBuilder app)
	{
		var bot = app.ApplicationServices.GetRequiredService<Bot>();
		var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

		lifetime.ApplicationStopped.Register(async _ => await bot.Stop(), bot);
	}
}
