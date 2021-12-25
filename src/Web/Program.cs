using DigitalOcean.API;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HexacowBot.Core.DiscordBot;
using Infrastructure;

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
		
		services.AddInfrastructure(config);

		var app = builder.Build();

		app.WarmUpInfrastructure();

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
}
