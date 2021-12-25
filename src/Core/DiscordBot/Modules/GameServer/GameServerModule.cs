using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using HexacowBot.Core.GameServer;

namespace HexacowBot.Core.DiscordBot.Modules.GameServer;

public sealed partial class GameServerModule : InteractionModuleBase
{
	private const string OwnerError = "During development this bot is only usable by the creator.";
	private const string RetryPrompt = "Would you like to retry?";

	private DigitalOceanService Server { get; set; }
	private IConfiguration Configuration { get; set; }
	private ILogger Logger { get; set; }

	private List<IUserMessage> MessagesToDelete { get; set; }

	public GameServerModule(
		DigitalOceanService digitalOceanService,
		IConfiguration configuration,
		ILogger<GameServerModule> logger)
	{
		Server = digitalOceanService;
		Configuration = configuration;
		Logger = logger;

		MessagesToDelete = new List<IUserMessage>();
	}

	[ComponentInteraction("server-abort")]
	public async Task ServerAbortAsync()
	{
		var component = (Context.Interaction as SocketMessageComponent)!;

		MessagesToDelete.Add(await component.GetOriginalResponseAsync());
		await component.UpdateAsync(message =>
		{
			message.Content = "Aborted.";
			message.Components = new ComponentBuilder().Build();
		});
	}

	public override async void AfterExecute(ICommandInfo command)
	{
		foreach (var message in MessagesToDelete)
		{
			await message.DeleteAsync();
		}
	}

	private async Task RetryClearComponentInteraction(IDiscordInteraction interaction)
	{
		if (interaction is SocketMessageComponent)
		{
			// We should be attempting to retry, so we'll remove the original interaction
			var component = (interaction as SocketMessageComponent)!;

			var originalResponse = await component.GetOriginalResponseAsync();
			if (originalResponse is not null)
			{
				var disabledButton = new ComponentBuilder()
					.Build();

				await originalResponse.ModifyAsync(message =>
				{
					message.Content = "Retrying...";
					message.Components = disabledButton;
				});
			}
		}
	}

	private string GetElapsedFriendly(TimeSpan timespan)
	{
		if (timespan == TimeSpan.Zero)
		{
			return string.Empty;
		}

		return $"({Math.Round(timespan.TotalSeconds, 0)} sec)";
	}
}
