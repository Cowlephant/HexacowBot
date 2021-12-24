using DigitalOcean.API;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HexacowBot.Core.DiscordBot;
using HexacowBot.Core.GameServer;

namespace HexacowBot;
public class Program
{
	//public static Task Main(string[] args) => new Program().MainAsync();

	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		var services = builder.Services;
		var config = builder.Configuration;

		services.AddRazorPages();
		services.Configure<RazorPagesOptions>(options =>
		{
			options.RootDirectory = "/Features";
		});

		services.AddServerSideBlazor();
		services.AddSingleton<DigitalOceanClient>(_ =>
		{
			var apiToken = config["DigitalOcean:ApiToken"];
			var client = new DigitalOceanClient(apiToken);

			return client;
		});
		services.AddSingleton<Bot>();
		services.AddSingleton<DiscordSocketClient, BotClient>(ServiceCollection => new BotClient(
			new DiscordSocketConfig
			{
				UseInteractionSnowflakeDate = false
			},
			config,
			ServiceCollection.GetRequiredService<ILogger<BotClient>>()));
		services.AddSingleton<InteractionService>(serviceCollection => new InteractionService(
			serviceCollection.GetService<DiscordSocketClient>(),
			new InteractionServiceConfig
			{
				DefaultRunMode = Discord.Interactions.RunMode.Async,
				ThrowOnError = true,
				LogLevel = Discord.LogSeverity.Verbose
			}));
		services.AddSingleton<CommandService>(new CommandService(
			new CommandServiceConfig
			{
				DefaultRunMode = Discord.Commands.RunMode.Async,
				ThrowOnError = true,
				CaseSensitiveCommands = false,
				LogLevel = Discord.LogSeverity.Verbose
			}));
		services.AddSingleton<CommandHandler>();
		services.AddSingleton<DigitalOceanService>();

		var app = builder.Build();
		WarmupServices(app.Services, app.Lifetime);

		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Error");
			// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			app.UseHsts();
		}

		app.UseHttpsRedirection();

		app.UseDefaultFiles();
		app.UseStaticFiles();

		app.UseRouting();
		app.UseEndpoints(endpoints =>
		{
			endpoints.MapBlazorHub();
			app.MapFallbackToPage("/_Host");
		});

		app.Run();
	}

	private static void WarmupServices(IServiceProvider services, IHostApplicationLifetime lifetime)
	{
		var bot = services.GetRequiredService<Bot>();
		lifetime.ApplicationStopping.Register(async _ => await bot.Stop(), bot);
	}
}
