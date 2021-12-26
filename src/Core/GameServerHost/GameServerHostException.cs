namespace HexacowBot.Core.GameServerHost;

public sealed class GameServerHostException : Exception
{
	public GameServerHostException()
	{
	}

	public GameServerHostException(string message) : base(message)
	{
	}

	public GameServerHostException(string message, Exception inner) : base(message, inner)
	{
	}
}
