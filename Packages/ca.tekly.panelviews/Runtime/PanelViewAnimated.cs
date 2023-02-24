using Tekly.Common.Tweenimation;
using UnityEngine;

namespace Tekly.PanelViews
{
    public class PanelViewAnimated : PanelView
    {
        [SerializeField] private Tweenimator m_showTween;
        [SerializeField] private Tweenimator m_hideTween;
        
        protected override void OnShow(PanelData panelData)
        {
            if (m_hideTween != null) {
                m_hideTween.Stop();
            }
            
            if (m_showTween != null) {
                m_showTween.Play();
            }
        }

        protected override void OnHide()
        {
            if (m_showTween != null) {
                m_showTween.Stop();
            }
            
            if (m_hideTween != null) {
                m_hideTween.Play();
            }
        }

        protected virtual void Update()
        {
            if (State == PanelState.Showing && (m_showTween == null || !m_showTween.IsPlaying)) {
                CompleteShow();
            } 
            
            if (State == PanelState.Hiding && (m_hideTween == null || !m_hideTween.IsPlaying)) {
                CompleteHide();
            } 
        }
    }
}