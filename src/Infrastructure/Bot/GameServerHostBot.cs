using Discord;
using Discord.WebSocket;
using HexacowBot.Core.DiscordBot;
using HexacowBot.Core.GameServerHost;

namespace HexacowBot.Infrastructure.Bot;

internal sealed class GameServerHostBot : IDisposable
{
	private readonly DiscordSocketClient client;
	private readonly CommandHandler commandHandler;
	private readonly IGameServerHost gameServerHost;
	private readonly ILogger<GameServerHostBot> logger;

	public GameServerHostBot(
		DiscordSocketClient client,
		CommandHandler commandHandler,
		IGameServerHost gameServerHost,
		ILogger<GameServerHostBot> logger)
	{
		this.client = client;
		this.commandHandler = commandHandler;
		this.gameServerHost = gameServerHost;
		this.logger = logger;

		Start();
	}

	public async void Dispose()
	{
		await Stop();
	}

	private async void Start()
	{
		client.Ready += HandleClientReady;
		client.Log += HandleLog;
		await commandHandler.InstallCommandsAsync();
	}

	public async Task Stop()
	{
		if (client is not null)
		{
			await client.LogoutAsync();
			await client.StopAsync();
		}
	}

	private async Task HandleClientReady()
	{
		await commandHandler.RegisterSlashCommands();

		await GameServerStatusHelper.SetServerStatus(client, gameServerHost);
	}

	private async Task HandleLog(LogMessage logMessage)
	{
		var severity = LoggingMapper.LogSeverityToLogLevel(logMessage.Severity);
		logger.Log(severity, logMessage.Message);
		await Task.CompletedTask;
	}
}
