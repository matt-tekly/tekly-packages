using System;
using System.Threading;

namespace Tekly.Tracing
{
	[Serializable]
	public struct TraceEventArg
	{
		public string Name;
		public string Value;

		public static TraceEventArg Create<T>(string name, T value)
		{
			return new TraceEventArg {
				Name = name, 
				Value = value.ToString()
			};
		}
	}
	
	[Serializable]
	public struct TraceEvent : IComparable<TraceEvent>, IComparable
	{
		public string Name;
		public string Category;

		public int StartFrame;
		public int EndFrame;

		public double StartTime;
		public double EndTime;
		
		public int Id;

		public string Process;
		public string ThreadName;
		public int ThreadId;
		public TraceEventArg[] Args;
		
		private static int s_eventId;
		

		internal static TraceEvent Create(TraceEventArg[] args = null)
		{
			return new TraceEvent {
				Id = Interlocked.Increment(ref s_eventId),
				ThreadName = Thread.CurrentThread.Name,
				ThreadId = Thread.CurrentThread.ManagedThreadId,
				Args = args
			};
		}
		
		public int CompareTo(TraceEvent other)
		{
			return StartTime.CompareTo(other.StartTime);
		}

		public int CompareTo(object obj)
		{
			if (obj is TraceEvent frameEvent) {
				return CompareTo(frameEvent);
			}

			return 0;
		}
	}
}