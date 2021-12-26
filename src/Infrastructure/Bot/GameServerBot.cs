using Discord.WebSocket;
using HexacowBot.Core.DiscordBot;
using HexacowBot.Core.GameServer;

namespace HexacowBot.Infrastructure.Bot;

internal sealed class GameServerBot : IDisposable
{
	private readonly DiscordSocketClient client;
	private readonly CommandHandler commandHandler;
	private readonly IGameServer gameServer;

	public GameServerBot(DiscordSocketClient client, CommandHandler commandHandler, IGameServer gameServer)
	{
		this.client = client;
		this.commandHandler = commandHandler;
		this.gameServer = gameServer;

		Start();
	}

	public async void Dispose()
	{
		await Stop();
	}

	private async void Start()
	{
		client.Ready += HandleClientReady;
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

		await GameServerStatusHelper.SetServerStatus(client, gameServer);
	}
}
