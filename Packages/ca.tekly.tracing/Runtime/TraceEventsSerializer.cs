using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace Tekly.Tracing
{
	public static class TraceEventsSerializer
	{
		[MenuItem("Tools/Tekly/Trace Events", priority = 0)]
		public static void Perfetto()
		{
			TraceEvents.ProcessEvents(evts => {
				var json = ToPerfetto(evts);
				File.WriteAllText("traceevents.json", json);
			});
		}
		
		public static string ToPerfetto(List<TraceEvent> source)
		{
			var sb = new StringBuilder();
			sb.Append("[\n");

			for (int i = 0; i < source.Count; i++)
			{
				var e = source[i];
				var ts = e.StartTime * 1000.0;
				var dur = (e.EndTime - e.StartTime) * 1000.0;
				var name = Escape(e.Name);
				var cat = Escape(e.Category ?? "default");
				var tid = Escape(e.ThreadName ?? $"Thread {e.ThreadId}");

				sb.Append("  {\n");
				sb.AppendFormat("    \"name\": \"{0}\",\n", name);
				sb.AppendFormat("    \"cat\": \"{0}\",\n", cat);
				sb.Append("    \"ph\": \"X\",\n");
				sb.AppendFormat("    \"ts\": {0},\n", ts.ToString("F4"));
				sb.AppendFormat("    \"dur\": {0},\n", dur.ToString("F4"));
				sb.Append("    \"pid\": 1,\n");
				sb.AppendFormat("    \"tid\": \"{0}\"", tid);

				// Only add args if present
				if (e.Args != null && e.Args.Length > 0)
				{
					sb.Append(",\n    \"args\": {");
					for (int j = 0; j < e.Args.Length; j++)
					{
						var arg = e.Args[j];
						sb.AppendFormat("\"{0}\": \"{1}\"", Escape(arg.Name), Escape(arg.Value));
						if (j < e.Args.Length - 1)
						{
							sb.Append(", ");
						}
					}
					sb.Append("}");
				}

				sb.Append("\n  }");

				if (i < source.Count - 1)
				{
					sb.Append(",");
				}

				sb.Append("\n");
			}

			sb.Append("]\n");
			return sb.ToString();
		}

		private static string Escape(string value)
		{
			if (string.IsNullOrEmpty(value)) {
				return "";
			}

			return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
		}
	}
}