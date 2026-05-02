using System;
using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafToggle : Toggle
	{
		[SerializeField] private LeafAnimator m_animator;
		
		private LeafNavigationElement m_leaf;

		protected override void Awake()
		{
			base.Awake();
			m_leaf = GetComponent<LeafNavigationElement>();
		}

		public override void OnMove(AxisEventData eventData)
		{
			if (m_leaf != null) {
				m_leaf.TryNavigate(eventData);	
			}
		}
		
		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			if (m_animator == null) {
				base.DoStateTransition(state, instant);	
			} else {
				m_animator.HandleMode(Convert(state), isOn, instant);
			}
		}

		private static LeafElementMode Convert(SelectionState state)
		{
			return state switch {
				SelectionState.Normal => LeafElementMode.Normal,
				SelectionState.Highlighted => LeafElementMode.Highlighted,
				SelectionState.Pressed => LeafElementMode.Pressed,
				SelectionState.Selected => LeafElementMode.Selected,
				SelectionState.Disabled => LeafElementMode.Disabled,
				_ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
			};
		}
	}
}