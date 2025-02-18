using System;

namespace Tekly.DebugKit.Widgets
{
	public class Updater : Widget
	{
		private readonly Action m_update;
		private readonly Func<bool> m_canUpdate;

		public Updater(Action update, Func<bool> canUpdate = null)
		{
			m_update = update;
			m_canUpdate = canUpdate;
		}
		
		public override void Update()
		{
			if (m_canUpdate == null || m_canUpdate.Invoke()) {
				m_update();
			}
		}
	}
}