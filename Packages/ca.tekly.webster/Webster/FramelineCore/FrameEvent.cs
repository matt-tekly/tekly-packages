//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Threading;

namespace Tekly.Webster.FramelineCore
{
	[Serializable]
	public struct FrameEvent : IComparable<FrameEvent>, IComparable
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

		public static FrameEvent Create()
		{
			return new FrameEvent {
				FrameEventId = Interlocked.Increment(ref s_frameEventId),
				ThreadName = Thread.CurrentThread.Name,
				ThreadId = Thread.CurrentThread.ManagedThreadId
			};
		}
		
		public int CompareTo(FrameEvent other)
		{
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