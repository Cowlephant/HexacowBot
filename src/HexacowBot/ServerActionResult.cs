namespace HexacowBot
{
	public record class ServerActionResult (bool Success, string Message, LogLevel severity)
	{
	}
}