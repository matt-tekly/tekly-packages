using Tekly.Logging;
using Tekly.PanelViews;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace Tekly.Extensions.PanelViews
{
    public class PanelViewActivity : InjectableActivity
    {
        [SerializeField] private string m_panelId;
        [SerializeField] private bool m_showOnLoad;
        [SerializeField] private bool m_hideOnLeave;

        private PanelView m_panelView;
        private TkLogger m_logger = TkLogger.Get<PanelViewActivity>();
		
        protected override void LoadingStarted()
        {
            if (PanelViewRegistry.Instance.TryGet(m_panelId, out m_panelView)) {
                if (m_showOnLoad) {
                    m_panelView.Show();	
                }
            } else {
                m_logger.Error("Failed to find panel [{panel}]", ("panel", m_panelId));		
            }
        }
		
        protected override bool IsDoneLoading()
        {
            return !m_showOnLoad || m_panelView.State == PanelState.Shown;
        }
		
        protected override void UnloadingStarted()
        {
            if (m_hideOnLeave) {
                m_panelView.Hide();	
            }
        }
		
        protected override bool IsDoneUnloading()
        {
            return !m_hideOnLeave || m_panelView.State == PanelState.Hidden;
        }
    }
}