using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tekly.Tracing
{
	public class PerfettoTraceEvent
	{
		[JsonProperty("name")] public string Name;
		[JsonProperty("cat")] public string Category;
		[JsonProperty("ph")] public string Phase;
		[JsonProperty("ts")] public double Timestamp;
		[JsonProperty("dur")] public double Duration;
		[JsonProperty("pid")] public int ProcessId;
		[JsonProperty("tid")] public string ThreadId;
		[JsonProperty("args")] public Dictionary<string, string> Args;

		public static List<PerfettoTraceEvent> Convert(List<TraceEvent> source)
		{
			var events = new List<PerfettoTraceEvent>();
			var processIds = new Dictionary<string, int>();
			int nextPid = 2; // reserve 1 for default

			foreach (var e in source)
			{
				if (!string.IsNullOrEmpty(e.Process) && !processIds.ContainsKey(e.Process))
				{
					processIds[e.Process] = nextPid++;
				}
			}

			// Add process_name metadata events
			foreach (var kvp in processIds)
			{
				events.Add(new PerfettoTraceEvent()
				{
					Name = "process_name",
					Phase = "M",
					ProcessId = kvp.Value,
					Args = new Dictionary<string, string> { { "name", kvp.Key } }
				});
			}

			// Add main trace events
			foreach (var e in source)
			{
				int pid = 1;
				if (!string.IsNullOrEmpty(e.Process) && processIds.TryGetValue(e.Process, out var customPid))
				{
					pid = customPid;
				}

				events.Add(new PerfettoTraceEvent()
				{
					Name = e.Name,
					Category = e.Category ?? "default",
					Phase = "X",
					Timestamp = e.StartTime * 1000.0,
					Duration = (e.EndTime - e.StartTime) * 1000.0,
					ProcessId = pid,
					ThreadId = e.ThreadName ?? $"Thread {e.ThreadId}",
					Args = TraceEventArgs(e)
				});
			}

			return events;
		}
		
		private static Dictionary<string, string> TraceEventArgs(TraceEvent traceEvent)
        {
            var dict = new Dictionary<string, string>();
            
            if (traceEvent.Args != null)
            {
                foreach (var arg in traceEvent.Args)
                {
                    dict[arg.Name] = arg.Value;
                }	
            }

            dict["Frames"] = $"{traceEvent.StartFrame} - {traceEvent.EndFrame}";
            
            return dict;
        }
	}
}