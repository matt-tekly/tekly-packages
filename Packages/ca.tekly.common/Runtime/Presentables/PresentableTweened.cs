using Tekly.Common.Tweenimation;
using UnityEngine;

namespace Tekly.Common.Presentables
{
	public class PresentableTweened : Presentable
	{
		[SerializeField] private Tweenimator m_showTween;
		[SerializeField] private Tweenimator m_hideTween;
		
		[Tooltip("Complete the hide tween when the presentable is disabled")]
		[SerializeField] private bool m_completeHideTweenOnDisable;
        
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

		protected override void OnDisable()
		{
			base.OnDisable();
			if (m_hideTween != null && m_completeHideTweenOnDisable) {
				m_hideTween.Complete();
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