using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace HexacowBot.Core.DiscordBot;

public sealed class RequireRoleAttribute : PreconditionAttribute
{
	private string Name { get; set; }

	public RequireRoleAttribute(string name)
	{
		Name = name;
	}

	public override async Task<PreconditionResult> CheckRequirementsAsync(
		IInteractionContext context,
		ICommandInfo commandInfo,
		IServiceProvider services)
	{
		var user = context.User as SocketGuildUser;
		var userCanExecute = user?.Roles.Any(r => r.Name.ToLowerInvariant() == Name.ToLowerInvariant()) ?? false;

		if (userCanExecute)
		{
			return await Task.FromResult(PreconditionResult.FromSuccess());
		}
		else
		{
			var failReason = $"You must be in the role ({Name}) to use this command.";
			return await Task.FromResult(PreconditionResult.FromError(failReason));
		}
	}
}
