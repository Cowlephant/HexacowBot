namespace HexacowBot.Core.GameServer;

public sealed class GameServerException : Exception
{
	public GameServerException()
	{
	}

	public GameServerException(string message) : base(message)
	{
	}

	public GameServerException(string message, Exception inner) : base(message, inner)
	{
	}
}
