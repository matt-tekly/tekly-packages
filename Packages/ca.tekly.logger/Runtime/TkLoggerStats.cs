using System;

namespace Tekly.Logging
{
	public class TkLoggerStats
	{
		public int Debug;
		public int Info;
		public int Warning;
		public int Error;
		public int Exception;

		public void Clear()
		{
			Debug = 0;
			Info = 0;
			Warning = 0;
			Error = 0;
			Exception = 0;
		}

		public void Track(TkLogMessage message)
		{
			switch (message.Level) {
				case TkLogLevel.Debug:
					Debug++;
					break;
				case TkLogLevel.Info:
					Info++;
					break;
				case TkLogLevel.Warning:
					Warning++;
					break;
				case TkLogLevel.Error:
					Error++;
					break;
				case TkLogLevel.Exception:
					Exception++;
					break;
				case TkLogLevel.None:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}