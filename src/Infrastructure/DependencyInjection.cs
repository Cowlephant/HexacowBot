using DigitalOcean.API;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using HexacowBot.Core.GameServer;
using HexacowBot.Infrastructure.Bot;
using HexacowBot.Infrastructure.GameServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton(serviceCollection =>
		{
			var logger = serviceCollection.GetRequiredService<ILogger<DiscordSocketClient>>();
			var botToken = configuration["Discord:BotToken"];

			DiscordSocketClient client = new DiscordSocketClient(new DiscordSocketConfig
			{
				UseInteractionSnowflakeDate = false,
				GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers
			});

			client.Log += logMessage =>
			{
				var severity = LoggingMapper.LogSeverityToLogLevel(logMessage.Severity);
				logger.Log(severity, logMessage.Message);
				return Task.CompletedTask;
			};

			client.LoginAsync(TokenType.Bot, botToken, validateToken: true).Wait();
			client.StartAsync().Wait();

			return client;
		});

		services.AddSingleton(serviceCollection => new InteractionService(
			serviceCollection.GetService<DiscordSocketClient>(),
			new InteractionServiceConfig
			{
				DefaultRunMode = Discord.Interactions.RunMode.Async,
				ThrowOnError = true,
				LogLevel = LogSeverity.Verbose
			}));

		services.AddSingleton(new CommandService(
			new CommandServiceConfig
			{
				DefaultRunMode = Discord.Commands.RunMode.Async,
				ThrowOnError = true,
				CaseSensitiveCommands = false,
				LogLevel = LogSeverity.Verbose
			}));

		services.AddSingleton<CommandHandler>();

		services.AddSingleton<IGameServer, DigitalOceanService>();

		services.AddSingleton(_ =>
		{
			var apiToken = configuration["DigitalOcean:ApiToken"];
			var client = new DigitalOceanClient(apiToken);

			return client;
		});

		services.AddSingleton<GameServerBot>();

		return services;
	}

	public static void WarmUpInfrastructure(this IApplicationBuilder app)
	{
		var gameServerBot = app.ApplicationServices.GetRequiredService<GameServerBot>();
		var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

		lifetime.ApplicationStopped.Register(async _ => await gameServerBot.Stop(), gameServerBot);
	}
}
