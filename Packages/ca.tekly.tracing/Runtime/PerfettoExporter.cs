using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Tekly.Tracing
{
	public static class PerfettoExporter
	{
		
#if UNITY_EDITOR
		[UnityEditor.MenuItem("Tools/Tekly/Trace Events", priority = 0)]
		public static void ToTraceEventsMenu()
		{
			TraceEvents.ProcessEvents(evts => {
				var json = ToJson(evts);
				Directory.CreateDirectory("Traces");
				File.WriteAllText("Traces/traceevents.json", json);
			});
		}
#endif
		
		public static string ToJson(List<TraceEvent> source)
		{
			var events = PerfettoTraceEvent.Convert(source);
			return JsonConvert.SerializeObject(events, Formatting.Indented);
		}
	}
}