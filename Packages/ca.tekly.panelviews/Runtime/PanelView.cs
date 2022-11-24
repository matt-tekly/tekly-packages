using UnityEngine;

namespace Tekly.PanelViews
{
    public enum PanelState
    {
        Hidden,
        Showing,
        Shown,
        Hiding
    }
    
    public class PanelView : MonoBehaviour
    {
        [SerializeField] private string m_id;
        
        protected PanelState m_state;

        public string Id => m_id;
        public PanelState State => m_state;
        public bool IsAnimating => m_state == PanelState.Showing || m_state == PanelState.Hiding;

        public void Show()
        {
            m_state = PanelState.Showing;
            OnShow();
        }

        public void Hide()
        {
            m_state = PanelState.Hiding;
            OnHide();
        }

        protected virtual void OnShow()
        {
            
        }

        protected virtual void OnHide()
        {
            
        }
    }
}