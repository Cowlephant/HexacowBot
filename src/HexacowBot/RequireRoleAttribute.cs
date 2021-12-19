using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace HexacowBot;

public sealed class RequireRoleAttribute : PreconditionAttribute
{
	private string name;

	public RequireRoleAttribute(string name)
	{
		this.name = name;
	}

	public override async Task<PreconditionResult> CheckRequirementsAsync(
		IInteractionContext context,
		ICommandInfo commandInfo,
		IServiceProvider services)
	{
		var user = await context.Client.GetUserAsync(context.User.Id) as SocketGuildUser;
		var userCanExecute = user?.Roles.Any(r => r.Name.ToLowerInvariant() == name.ToLowerInvariant()) ?? false;

		if (userCanExecute)
		{
			return await Task.FromResult(PreconditionResult.FromSuccess());
		}
		else
		{
			var failReason = $"You must be in the role ({name}) to use this command.";
			return await Task.FromResult(PreconditionResult.FromError(failReason));
		}
	}
}
