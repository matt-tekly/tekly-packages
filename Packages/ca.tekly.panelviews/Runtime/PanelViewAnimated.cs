using Tekly.Common.Tweenimation;
using UnityEngine;

namespace Tekly.PanelViews
{
    public class PanelViewAnimated : PanelView
    {
        [SerializeField] private Tweenimator m_showTween;
        [SerializeField] private Tweenimator m_hideTween;
        
        protected override void OnShow()
        {
            m_hideTween.Stop();
            m_showTween.Play();
        }

        protected override void OnHide()
        {
            m_showTween.Stop();
            m_hideTween.Play();
        }

        protected virtual void Update()
        {
            if (m_state == PanelState.Showing && !m_showTween.IsPlaying) {
                m_state = PanelState.Shown;
            } 
            
            if (m_state == PanelState.Hiding && !m_hideTween.IsPlaying) {
                m_state = PanelState.Hidden;
            } 
        }
    }
}