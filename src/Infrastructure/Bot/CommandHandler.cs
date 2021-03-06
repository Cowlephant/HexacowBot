using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using HexacowBot.Core.DiscordBot.Modules.GameServerHost;
using System.Reflection;

namespace HexacowBot.Infrastructure.Bot;

internal sealed class CommandHandler
{
	private readonly DiscordSocketClient client;
	private readonly CommandService commandService;
	private readonly InteractionService interactionService;
	private readonly IServiceProvider serviceProvider;
	private readonly IConfiguration configuration;

	public CommandHandler(
		DiscordSocketClient client,
		CommandService commandService,
		InteractionService interactionService,
		IServiceProvider serviceProvider,
		IConfiguration configuration)
	{
		this.client = client;
		this.commandService = commandService;
		this.interactionService = interactionService;
		this.serviceProvider = serviceProvider;
		this.configuration = configuration;
	}

	public async Task RegisterSlashCommands()
	{
		ulong guildId = ulong.Parse(configuration["Discord:GuildId"]);

		await interactionService.RegisterCommandsToGuildAsync(guildId, deleteMissing: true);
	}

	public async Task InstallCommandsAsync()
	{
		Console.WriteLine("Finding and installing commands.");
		client.MessageReceived += HandleCommandAsync;
		client.SlashCommandExecuted += HandleInteractionsAsync;
		client.ButtonExecuted += HandleComponentInteractionsAsync;
		client.SelectMenuExecuted += HandleComponentInteractionsAsync;

		var assembly = Assembly.GetAssembly(typeof(GameServerHostModule));
		await commandService.AddModulesAsync(assembly, serviceProvider);
		await interactionService.AddModulesAsync(assembly, serviceProvider);
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

		await commandService.ExecuteAsync(commandContext, argumentsPosition, serviceProvider);
	}

	private async Task HandleInteractionsAsync(SocketSlashCommand command)
	{
		var interactionContext = new InteractionContext(client, command, command.User, command.Channel);
		await interactionService.ExecuteCommandAsync(interactionContext, serviceProvider);
	}

	private async Task HandleComponentInteractionsAsync(SocketMessageComponent component)
	{
		IInteractionContext context;
		if (component is SocketMessageComponent)
		{
			context = new SocketInteractionContext<SocketMessageComponent>(client, component);
		}
		else
		{
			throw new NotImplementedException();
		}

		await interactionService.ExecuteCommandAsync(context, serviceProvider);
	}
}
