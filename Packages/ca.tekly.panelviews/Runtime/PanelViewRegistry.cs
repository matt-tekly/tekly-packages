using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine.Assertions;

namespace Tekly.PanelViews
{
    public class PanelViewRegistry : Singleton<PanelViewRegistry>
    {
        private Dictionary<string, PanelView> m_panelViews = new Dictionary<string, PanelView>();
        
        public void Register(PanelView panelView)
        {
            Assert.IsFalse(m_panelViews.ContainsKey(panelView.Id));
            m_panelViews[panelView.Id] = panelView;
        }

        public void Remove(PanelView panelView)
        {
            m_panelViews.Remove(panelView.Id);
        }

        public bool TryGet(string id, out PanelView panelView)
        {
            return m_panelViews.TryGetValue(id, out panelView);
        }
        
        public bool TryGet<T>(string id, out T panelView) where T : PanelView
        {
            if (m_panelViews.TryGetValue(id, out var target)) {
                panelView = target as T;
                return true;
            }

            panelView = null;
            return false;
        }
    }
}