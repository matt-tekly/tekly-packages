using System;

namespace Tekly.Logging
{
	public static class TkLoggerUtils
	{
		public static string LevelToCharacter(TkLogLevel level)
		{
			switch (level) {
				case TkLogLevel.Debug:
					return "[D]";
				case TkLogLevel.Info:
					return "[I]";
				case TkLogLevel.Warning:
					return "[W]";
				case TkLogLevel.Error:
					return "[E]";
				case TkLogLevel.Exception:
					return "[X]";
				default:
					throw new ArgumentOutOfRangeException(nameof(level), level, null);
			}
		}
	}
}