using Discord.WebSocket;

namespace HexacowBot;

public sealed class Bot : IDisposable
{
	private readonly DiscordSocketClient client;
	private readonly CommandHandler commandHandler;

	public Bot(DiscordSocketClient client, CommandHandler commandHandler)
	{
		this.client = client;
		this.commandHandler = commandHandler;

		Start();
	}

	public async void Dispose()
	{
		await Stop();
	}

	private async void Start()
	{
		client.Ready += RegisterSlashCommands;
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

	private async Task RegisterSlashCommands()
	{
		await commandHandler.RegisterSlashCommands();
	}
}
