using System.Collections.Generic;
using Tekly.Content;
using UnityEngine;

namespace Tekly.Glass
{
    public enum PanelSource
    {
        Embedded,
        Addressable
    }

    public class PanelsProvider : MonoBehaviour
    {
        public Transform PoolContainer;
        
        [SerializeField] private Panel[] m_panels;
        
        private IContentProvider m_contentProvider;
        private Dictionary<string, PanelProvider> m_loadedPanels;

        private void Awake()
        {
            m_contentProvider = ContentProvider.Instance;
            m_loadedPanels = new Dictionary<string, PanelProvider>(m_panels.Length);
            
            foreach (var panel in m_panels) {
                m_loadedPanels.Add(panel.name, new PanelProvider(panel, this));
            }
        }

        public PanelProvider Get(string id)
        {
            if (!m_loadedPanels.TryGetValue(id, out var loadedPanel)) {
                loadedPanel = new PanelProvider(id, this, m_contentProvider);
                m_loadedPanels.Add(id, loadedPanel);
            }

            return loadedPanel;
        }
    }
}