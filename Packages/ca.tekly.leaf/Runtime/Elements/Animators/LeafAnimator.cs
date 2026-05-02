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
		
		public void HandleMode(LeafElementMode mode, bool on, bool instant)
		{
			OnHandleMode(mode, m_previousMode, on, instant);
			m_previousMode = mode;
		}
		
		protected abstract void OnHandleMode(LeafElementMode current, LeafElementMode previous, bool on, bool instant);
	}
}