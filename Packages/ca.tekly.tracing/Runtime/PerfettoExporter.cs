using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;

namespace Tekly.Tracing
{
	public static class PerfettoExporter
	{
		[MenuItem("Tools/Tekly/Trace Events", priority = 0)]
		public static void ToTraceEventsMenu()
		{
			TraceEvents.ProcessEvents(evts => {
				var json = ToJson(evts);
				File.WriteAllText("traceevents.json", json);
			});
		}

		public static string ToJson(List<TraceEvent> source)
		{
			var events = PerfettoTraceEvent.Convert(source);
			return JsonConvert.SerializeObject(events, Formatting.Indented);
		}
	}
}