using Tekly.Common.Tweenimation;
using UnityEngine;

namespace Tekly.Common.Presentables
{
	public class PresentableTweened : Presentable
	{
		[SerializeField] private Tweenimator m_showTween;
		[SerializeField] private Tweenimator m_hideTween;
        
		protected override void OnShow()
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
			if (State == PresentableState.Showing && (m_showTween == null || !m_showTween.IsPlaying)) {
				CompleteShow();
			} 
            
			if (State == PresentableState.Hiding && (m_hideTween == null || !m_hideTween.IsPlaying)) {
				CompleteHide();
			} 
		}
	}
}