namespace HexacowBot.Core.GameServerHost;

public record class ServerActionResult(bool Success, string Message, TimeSpan elapsedTime, LogLevel Severity)
{
}
