using System.Collections.Generic;
using System.Threading.Tasks;
using Tekly.Content;
using UnityEngine;

namespace Tekly.Glass
{
    public enum PanelSource
    {
        Embedded,
        Addressable
    }

    public class PanelProvider : MonoBehaviour
    {
        [SerializeField] private Panel[] m_panels;
        
        private IContentProvider m_contentProvider;
        private Dictionary<string, LoadedPanel> m_loadedPanels;

        private void Awake()
        {
            m_contentProvider = ContentProvider.Instance;
            m_loadedPanels = new Dictionary<string, LoadedPanel>(m_panels.Length);
            
            foreach (var panel in m_panels) {
                m_loadedPanels.Add(panel.name, new LoadedPanel(panel));
            }
        }

        public async Task<LoadedPanel> Get(string id)
        {
            if (!m_loadedPanels.TryGetValue(id, out var loadedPanel)) {
                loadedPanel = new LoadedPanel(id, m_contentProvider);
                m_loadedPanels.Add(id, loadedPanel);
            }

            return await loadedPanel;
        }
    }
}