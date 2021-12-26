using DigitalOcean.API;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using HexacowBot.Core.GameServerHost;
using HexacowBot.Infrastructure.Bot;
using HexacowBot.Infrastructure.GameServerHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		ConfigureBot(services, configuration);
		ConfigureGameServerHost(services, configuration);

		return services;
	}

	public static void WarmUpInfrastructure(this IApplicationBuilder app)
	{
		var gameServerHostBot = app.ApplicationServices.GetRequiredService<GameServerHostBot>();
		var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

		lifetime.ApplicationStopped.Register(async _ => await gameServerHostBot.Stop(), gameServerHostBot);
	}

	private static IServiceCollection ConfigureBot(IServiceCollection services, IConfiguration configuration)
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

		services.AddSingleton<GameServerHostBot>();

		return services;
	}

	private static IServiceCollection ConfigureGameServerHost(IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IGameServerHost, DigitalOceanService>();

		services.AddSingleton(_ =>
		{
			var apiToken = configuration["DigitalOcean:ApiToken"];
			var client = new DigitalOceanClient(apiToken);

			return client;
		});

		return services;
	}
}
