using System;

namespace Tekly.Webster.FramelineCore
{
	public readonly struct FramelineEventDisposable : IDisposable
	{
		public static FramelineEventDisposable BeginEvent(string id, string type)
		{
			var eventId = Frameline.BeginEventGetId(id, type);
			return new FramelineEventDisposable(eventId);
		}

		private readonly int m_id;

		private FramelineEventDisposable(int eventId)
		{
			m_id = eventId;
		}

		public void Dispose()
		{
			Frameline.EndEvent(m_id);
		}
	}
}