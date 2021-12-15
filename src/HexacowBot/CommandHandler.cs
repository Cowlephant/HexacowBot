using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace HexacowBot;

public sealed class CommandHandler
{
	private readonly DiscordSocketClient client;
	private readonly CommandService commands;
	private readonly IServiceProvider serviceProvider;

	public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider serviceProvider)
	{
		this.client = client;
		this.commands = commands;
		this.serviceProvider = serviceProvider;
	}

	public async Task InstallCommandsAsync()
	{
		Console.WriteLine("Finding and installing commands.");
		client.MessageReceived += HandleCommandAsync;

		await commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), serviceProvider);
	}

	private async Task HandleCommandAsync(SocketMessage messageParam)
	{
		// Ignore message if it's a system message
		var message = messageParam as SocketUserMessage;
		if (message is null)
		{
			return;
		}

		int argumentsPosition = 0;
		var isNotCommandOrIsBot =
			!(message.HasCharPrefix('!', ref argumentsPosition) ||
			message.HasMentionPrefix(client.CurrentUser, ref argumentsPosition)) ||
			message.Author.IsBot;
		if (isNotCommandOrIsBot)
		{
			return;
		}

		var commandContext = new SocketCommandContext(client, message);

		await commands.ExecuteAsync(commandContext, argumentsPosition, serviceProvider);
	}
}
