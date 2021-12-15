using Discord;
using Microsoft.Extensions.Logging;

namespace HexacowBot;

public sealed class LoggingMapper
{
	public static LogLevel LogSeverityToLogLevel(LogSeverity severity)
	{
		var logLevel = severity switch
		{
			LogSeverity.Critical => LogLevel.Critical,
			LogSeverity.Error => LogLevel.Error,
			LogSeverity.Warning => LogLevel.Warning,
			LogSeverity.Info => LogLevel.Information,
			LogSeverity.Debug => LogLevel.Debug,
			LogSeverity.Verbose => LogLevel.Trace,
			_ => LogLevel.Trace
		};

		return logLevel;
	}
}
