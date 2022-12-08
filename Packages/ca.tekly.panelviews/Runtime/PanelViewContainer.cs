using System;
using Tekly.Injectors;
using Tekly.Injectors.Utils;
using UnityEngine;

namespace Tekly.PanelViews
{
    public class PanelViewContainer : MonoBehaviour
    {
        [SerializeField] private PanelView[] m_panels = Array.Empty<PanelView>();
        [Inject] private InjectorContainer m_container;
        
        private void Awake()
        {
            foreach (var panelView in m_panels) {
                PanelViewRegistry.Instance.Register(panelView);

                if (m_container != null) {
                    m_container.Inject(panelView);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var panelView in m_panels) {
                PanelViewRegistry.Instance.Remove(panelView);    
            }
        }
        
        public void FindPanels()
        {
            m_panels = GetComponentsInChildren<PanelView>(true);
        }
    }
}