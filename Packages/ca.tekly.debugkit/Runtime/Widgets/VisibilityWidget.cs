using System;

namespace Tekly.DebugKit.Widgets
{
	public class VisibilityWidget : Widget
	{
		private readonly Container m_target;
		private readonly Func<bool> m_visible;

		public VisibilityWidget(Container target, Func<bool> visible)
		{
			m_target = target;
			m_visible = visible;
		}

		public override void Update()
		{
			m_target.Visible = m_visible();
		}
	}
}