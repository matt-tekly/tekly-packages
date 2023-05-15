using Tekly.Common.Observables;
using Tekly.Common.Presentables;
using Tekly.Common.Registrys;
using Tekly.Common.Utils;

namespace Tekly.PanelViews
{
    public struct PanelStateChangedEvt
    {
        public readonly string Id;
        public readonly PresentableState State;

        public PanelStateChangedEvt(string id, PresentableState state)
        {
            Id = id;
            State = state;
        }
    }
    
    public class PanelViewRegistry : SingletonRegistry<PanelView, PanelViewRegistry>
    {
        public ITriggerable<PanelStateChangedEvt> PanelStateChanged => m_panelStateChanged;
        
        private readonly Triggerable<PanelStateChangedEvt> m_panelStateChanged = new Triggerable<PanelStateChangedEvt>();
        
        public void OnPanelStateChanged(PanelView panelView)
        {
            m_panelStateChanged.Emit(new PanelStateChangedEvt(panelView.Id, panelView.State));
        }
    }
}