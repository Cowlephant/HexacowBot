using Microsoft.Extensions.Logging;

namespace HexacowBot.Core.GameServer;

public record class ServerActionResult(bool Success, string Message, TimeSpan elapsedTime, LogLevel Severity)
{
}
