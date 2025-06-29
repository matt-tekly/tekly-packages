using System;

namespace Tekly.Tracing
{
	public readonly struct TraceEventDisposable : IDisposable
	{
		public static TraceEventDisposable Unit = new TraceEventDisposable(-1);
		
		public static TraceEventDisposable BeginEvent(string name, string category)
		{
			var eventId = TraceEvents.BeginWithId(name, category);
			return new TraceEventDisposable(eventId);
		}

		private readonly int m_id;

		private TraceEventDisposable(int id)
		{
			m_id = id;
		}

		public void Dispose()
		{
			TraceEvents.End(m_id);
		}
	}
}