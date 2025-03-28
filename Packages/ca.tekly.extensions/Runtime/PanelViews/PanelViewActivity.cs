using Tekly.Common.Presentables;
using Tekly.EditorUtils.Attributes;
using Tekly.Injectors;
using Tekly.Injectors.Utils;
using Tekly.Logging;
using Tekly.PanelViews;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace Tekly.Extensions.PanelViews
{
    public class PanelViewActivity : InjectableActivity
    {
        [SerializeField] private string m_panelId;
        [SerializeField] private string m_context;
        [Polymorphic, SerializeReference] private PanelData m_panelData;
        
        [SerializeField] private bool m_showOnLoad;
        [SerializeField] private bool m_hideOnLeave;
        [SerializeField] private bool m_injectIntoPanel;

        [Inject] private InjectorContainer m_injectorContainer;
        
        private PanelView m_panelView;
        private TkLogger m_logger = TkLogger.Get<PanelViewActivity>();
		
        protected override void LoadingStarted()
        {
            if (PanelViewRegistry.Instance.TryGet(m_panelId, out m_panelView)) {
                if (m_showOnLoad) {
                    if (m_panelView.TryGetComponent(out HierarchyInjector hierarchyInjector)) {
                        hierarchyInjector.Inject(m_injectorContainer);
                    }
                    m_panelView.Show(m_context, m_panelData);	
                }
            } else {
                m_logger.Error("Failed to find panel [{panel}]", ("panel", m_panelId));		
            }
        }
		
        protected override bool IsDoneLoading()
        {
            return !m_showOnLoad || m_panelView.State == PresentableState.Shown;
        }
		
        protected override void UnloadingStarted()
        {
            if (m_hideOnLeave) {
                m_panelView.Hide();	
            }
        }
		
        protected override bool IsDoneUnloading()
        {
            return !m_hideOnLeave || m_panelView.State == PresentableState.Hidden;
        }
    }
}