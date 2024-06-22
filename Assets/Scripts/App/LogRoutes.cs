using DotLiquid;
using Tekly.Logging;
using Tekly.Tinker.Core;
using UnityEngine;

namespace TeklySample.App
{
	public class LogMessage : Drop
	{
		public string Message { get; set; }
		public LogType Type { get; set; }
	}

	[Route("/logs"), Description("Logs")]
	public class LogRoutes
	{
		[Post("/log"), Description("Test Log")]
		public LogMessage LogMessage([LargeText] string message, LogType logType = LogType.Log)
		{
			Debug.LogFormat(logType, LogOption.None, null, message);

			return new LogMessage {
				Message = message,
				Type = logType
			};
		}

		[Page("/all", "logs")]
		public LogMessage LogMessage()
		{
			return new LogMessage {
				Message = "Test Log",
				Type = LogType.Log
			};
		}

		[Page("/stats", "tinker_stats_card", "Stats")]
		public DataList Stats()
		{
			return new DataList("Logs")
				.Add("Debug", TkLogger.Stats.Debug)
				.Add("Info", TkLogger.Stats.Info)
				.Add("Warning", TkLogger.Stats.Warning, "yellow")
				.Add("Error", TkLogger.Stats.Error, "red")
				.Add("Exception", TkLogger.Stats.Exception, "red");
		}
	}
}