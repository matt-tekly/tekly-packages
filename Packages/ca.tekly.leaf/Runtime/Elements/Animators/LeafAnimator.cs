using UnityEngine;

namespace Tekly.Leaf.Elements.Animators
{
	public enum LeafElementMode
	{
		Normal,
		Highlighted,
		Pressed,
		Selected,
		Disabled,
	}
	
	public abstract class LeafAnimator : MonoBehaviour
	{
		private LeafElementMode m_previousMode = LeafElementMode.Normal;
		private bool m_previousOn;

		private bool m_hasModeSet;
		
		public void HandleMode(LeafElementMode mode, bool on, bool instant)
		{
            if (m_hasModeSet && mode == m_previousMode && m_previousOn == on) {
                return;
            }
                        
			OnHandleMode(mode, m_previousMode, on, instant);
			m_previousMode = mode;
			m_previousOn = on;

			m_hasModeSet = true;
		}
		
		protected abstract void OnHandleMode(LeafElementMode current, LeafElementMode previous, bool on, bool instant);
	}
}