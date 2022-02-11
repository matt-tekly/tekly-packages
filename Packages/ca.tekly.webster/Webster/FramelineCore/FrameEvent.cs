//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Threading;

namespace Tekly.Webster.FramelineCore
{
	[Serializable]
	public class FrameEvent : IComparable<FrameEvent>, IComparable
	{
		public string Id;

		public string EventType;

		public int StartFrame;
		public int EndFrame;

		public double StartTime;
		public double EndTime;

		/// <summary>
		/// Unique internal id of this FrameEvent
		/// </summary>
		public int FrameEventId;

		public string ThreadName;
		public int ThreadId;
		
		private static int s_frameEventId;

		public FrameEvent()
		{
			FrameEventId = Interlocked.Increment(ref s_frameEventId);
			ThreadName = Thread.CurrentThread.Name;
			ThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		public int CompareTo(FrameEvent other)
		{
			if (ReferenceEquals(this, other)) {
				return 0;
			}

			if (ReferenceEquals(null, other)) {
				return 1;
			}

			return StartTime.CompareTo(other.StartTime);
		}

		public int CompareTo(object obj)
		{
			if (obj is FrameEvent frameEvent) {
				return CompareTo(frameEvent);
			}

			return 0;
		}
	}
}