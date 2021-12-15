﻿using DigitalOcean.API;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
		services.AddSingleton<DiscordSocketClient, BotClient>();
		services.AddSingleton<CommandService>();
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
		lifetime.ApplicationStopping.Register(async _ => await bot.Stop() , bot);
	}
}
