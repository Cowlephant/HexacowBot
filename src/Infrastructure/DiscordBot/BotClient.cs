using Discord;
using Discord.WebSocket;

namespace HexacowBot.Infrastructure.DiscordBot;

public sealed class BotClient : DiscordSocketClient
{
	private readonly IConfiguration config;
	private readonly ILogger logger;

	public BotClient(DiscordSocketConfig clientConfig, IConfiguration config, ILogger<BotClient> logger)
		: base(clientConfig)
	{
		this.config = config;
		this.logger = logger;

		Initialize();
		Start();
	}

	private void Initialize()
	{
		Console.WriteLine("Initializing bot.");
		Log += LogHandler;
	}

	private async void Start()
	{
		Console.WriteLine("Starting bot.");
		var botToken = config["Discord:BotToken"];

		await LoginAsync(TokenType.Bot, botToken);
		await StartAsync();
	}

	private Task LogHandler(LogMessage logMessage)
	{
		var severity = LoggingMapper.LogSeverityToLogLevel(logMessage.Severity);
		logger.Log(severity, logMessage.Message);
		return Task.CompletedTask;
	}
}
