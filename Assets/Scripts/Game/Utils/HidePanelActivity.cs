using Tekly.Extensions.PanelViews;
using Tekly.Logging;
using Tekly.PanelViews;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace TeklySample.Game.Utils
{
	public class HidePanelActivity : InjectableActivity
	{
		[SerializeField] private string m_panelId;

		private PanelView m_panelView;
		private TkLogger m_logger = TkLogger.Get<PanelViewActivity>();

		protected override void LoadingStarted()
		{
			if (PanelViewRegistry.Instance.TryGet(m_panelId, out m_panelView)) {
				m_panelView.Hide();
			} else {
				m_logger.Error("Failed to find panel [{panel}]", ("panel", m_panelId));
			}
		}
	}
}