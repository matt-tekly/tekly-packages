using System.Collections.Generic;
using System.Data.Common;
using Tekly.Common.Observables;
using Tekly.Common.Utils;
using UnityEngine.Assertions;

namespace Tekly.PanelViews
{
    public struct PanelStateChangedEvt
    {
        public readonly string Id;
        public readonly PanelState State;

        public PanelStateChangedEvt(string id, PanelState state)
        {
            Id = id;
            State = state;
        }
    }
    
    public class PanelViewRegistry : Singleton<PanelViewRegistry>
    {
        public ITriggerable<PanelStateChangedEvt> PanelStateChanged => m_panelStateChanged;
        
        private readonly Dictionary<string, PanelView> m_panelViews = new Dictionary<string, PanelView>();
        private readonly Triggerable<PanelStateChangedEvt> m_panelStateChanged = new Triggerable<PanelStateChangedEvt>();

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

        public void OnPanelStateChanged(PanelView panelView)
        {
            m_panelStateChanged.Emit(new PanelStateChangedEvt(panelView.Id, panelView.State));
        }
    }
}