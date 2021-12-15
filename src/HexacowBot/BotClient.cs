using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace HexacowBot
{
	public sealed class BotClient : DiscordSocketClient
	{
		private readonly IConfiguration config;
		private readonly ILogger logger;
		public ulong GuildId { get; private set; }

		public BotClient(IConfiguration config, ILogger<BotClient> logger)
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

			GuildId = ulong.Parse(config["Discord:GuildId"]);
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
}