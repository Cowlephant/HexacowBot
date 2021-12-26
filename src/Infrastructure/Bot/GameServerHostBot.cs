using Discord.WebSocket;
using HexacowBot.Core.DiscordBot;
using HexacowBot.Core.GameServerHost;

namespace HexacowBot.Infrastructure.Bot;

internal sealed class GameServerHostBot : IDisposable
{
	private readonly DiscordSocketClient client;
	private readonly CommandHandler commandHandler;
	private readonly IGameServerHost gameServerHost;

	public GameServerHostBot(DiscordSocketClient client, CommandHandler commandHandler, IGameServerHost gameServerHost)
	{
		this.client = client;
		this.commandHandler = commandHandler;
		this.gameServerHost = gameServerHost;

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

		await GameServerStatusHelper.SetServerStatus(client, gameServerHost);
	}
}
