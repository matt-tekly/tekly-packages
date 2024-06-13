using DotLiquid;
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
		public LogMessage LogMessage(LogType logType, string message)
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
	}
}