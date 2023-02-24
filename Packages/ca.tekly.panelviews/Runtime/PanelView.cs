using System;
using UnityEngine;

namespace Tekly.PanelViews
{
    [Serializable]
    public class PanelData { }
    
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
        
        private PanelState m_state;

        public string Id => m_id;
        public PanelState State {
            get => m_state;
            set {
                if (m_state != value) {
                    m_state = value;
                    PanelViewRegistry.Instance.OnPanelStateChanged(this);
                }
            }
        }
        
        public bool IsAnimating => m_state == PanelState.Showing || m_state == PanelState.Hiding;
        public string Context { get; private set; }
        
        public void Show(string context, PanelData data = null)
        {
            if (m_state == PanelState.Shown || m_state == PanelState.Showing) {
                return;
            }

            Context = context;
            State = PanelState.Showing;
            gameObject.SetActive(true);
            OnShow(data);
        }

        public void Hide()
        {
            if (m_state == PanelState.Hidden || m_state == PanelState.Hiding) {
                return;
            }
            
            State = PanelState.Hiding;
            OnHide();
        }

        protected virtual void OnShow(PanelData panelData)
        {
            CompleteShow();
        }

        protected virtual void OnHide()
        {
            CompleteHide();
        }

        protected void CompleteShow()
        {
            State = PanelState.Shown;
        }

        protected void CompleteHide()
        {
            State = PanelState.Hidden;
            gameObject.SetActive(false);
        }
    }
}