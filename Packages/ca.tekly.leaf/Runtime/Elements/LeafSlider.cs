using System;
using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafSlider : Slider
	{
		private LeafNavigationElement m_leaf;
		[SerializeField] private LeafAnimator m_animator;
		
		protected override void Awake()
		{
			base.Awake();
			m_leaf = GetComponent<LeafNavigationElement>();
		}
		
		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			if (m_animator == null) {
				base.DoStateTransition(state, instant);	
			} else {
				m_animator.HandleMode(Convert(state), false, instant);
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

		public override void OnMove(AxisEventData eventData)
		{
			switch (eventData.moveDir) {
				case MoveDirection.Left:
				case MoveDirection.Right:
					base.OnMove(eventData);
					return;

				case MoveDirection.Up:
				case MoveDirection.Down:
					if (m_leaf != null && m_leaf.TryNavigate(eventData)) {
						return;
					}

					base.OnMove(eventData);
					return;
			}

			// base.OnMove(eventData);
		}
	}
}