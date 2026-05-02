using System;
using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafSelectable : Selectable
	{
		[Serializable]
		public class SelectableSelectedEvent : UnityEvent<bool> {}

		public SelectableSelectedEvent OnSelected
		{
			get => m_onSelected;
			set => m_onSelected = value;
		}
		
		[SerializeField] private SelectableSelectedEvent m_onSelected = new();
		[SerializeField] private LeafAnimator m_animator;

		private LeafNavigationElement m_leaf;

		protected override void Awake()
		{
			base.Awake();
			m_leaf = GetComponent<LeafNavigationElement>();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			
			if (EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject)
			{
				m_onSelected.Invoke(true);
			}
		}
		
		protected override void InstantClearState()
		{
			base.InstantClearState();
			m_onSelected.Invoke(false);
		}
		
		public override void OnSelect(BaseEventData eventData)
		{
			base.OnSelect(eventData);
			m_onSelected.Invoke(true);
		}
		
		public override void OnDeselect(BaseEventData eventData)
		{
			base.OnDeselect(eventData);
			m_onSelected.Invoke(false);
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
	}
}