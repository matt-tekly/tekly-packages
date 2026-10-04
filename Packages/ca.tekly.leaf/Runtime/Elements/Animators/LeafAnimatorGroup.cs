using System;
using UnityEngine;

namespace Tekly.Leaf.Elements.Animators
{
	/// <summary>
	/// Forwards state to several animators so one element can drive e.g. colors and sprites together.
	/// Assign the group as the element's animator.
	/// </summary>
	public class LeafAnimatorGroup : LeafAnimator
	{
		[Tooltip("Animators that receive every state this group gets")]
		[SerializeField] private LeafAnimator[] m_animators = Array.Empty<LeafAnimator>();

		protected override void OnHandleState(LeafElementState current, LeafElementState previous, bool instant)
		{
			for (var i = 0; i < m_animators.Length; i++) {
				var animator = m_animators[i];
				if (animator != null && animator != this) {
					animator.HandleState(current, instant);
				}
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			for (var i = 0; i < m_animators.Length; i++) {
				if (m_animators[i] == this) {
					Debug.LogWarning($"[{nameof(LeafAnimatorGroup)}] A group can't contain itself", this);
					m_animators[i] = null;
				}
			}
		}
#endif
	}
}
