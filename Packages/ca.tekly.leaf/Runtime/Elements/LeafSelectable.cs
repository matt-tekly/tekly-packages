using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafSelectable : Selectable
	{
		public SelectableSelectedEvent OnSelected => m_onSelected;
		public LeafElementState CurrentState => m_tracker.GetState(IsInteractable(), IsOnState);

		[SerializeField] private SelectableSelectedEvent m_onSelected = new();
		[SerializeField] private LeafAnimator m_animator;

		private readonly LeafStateTracker m_tracker = new();

		protected virtual bool IsOnState => false;

		protected override void OnEnable()
		{
			m_tracker.IsPointerDown = false;
			m_tracker.IsPressSimulated = false;
			m_tracker.IsSelected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;

			base.OnEnable();

			if (m_tracker.IsSelected) {
				m_onSelected.Invoke(true);
			}
		}

		protected override void InstantClearState()
		{
			m_tracker.Clear();
			base.InstantClearState();
			m_onSelected.Invoke(false);
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left) {
				m_tracker.IsPointerDown = true;
			}

			base.OnPointerDown(eventData);
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left) {
				m_tracker.IsPointerDown = false;
			}

			base.OnPointerUp(eventData);
		}

		public override void OnPointerEnter(PointerEventData eventData)
		{
			m_tracker.IsPointerInside = true;
			base.OnPointerEnter(eventData);
		}

		public override void OnPointerExit(PointerEventData eventData)
		{
			m_tracker.IsPointerInside = false;
			base.OnPointerExit(eventData);
		}

		public override void OnSelect(BaseEventData eventData)
		{
			m_tracker.IsSelected = true;
			base.OnSelect(eventData);
			m_onSelected.Invoke(true);
		}

		public override void OnDeselect(BaseEventData eventData)
		{
			m_tracker.IsSelected = false;
			base.OnDeselect(eventData);
			m_onSelected.Invoke(false);
		}

		public override void OnMove(AxisEventData eventData)
		{
			LeafNavigationScope.TryNavigateFrom(this, eventData);
		}

		/// <summary>
		/// Forces the Pressed mode on or off without pointer input (submit, delayed press).
		/// </summary>
		protected void SetPressSimulated(bool isPressed)
		{
			m_tracker.IsPressSimulated = isPressed;
			DoStateTransition(isPressed ? SelectionState.Pressed : currentSelectionState, false);
		}

		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			if (m_animator == null) {
				base.DoStateTransition(state, instant);
			} else {
				m_animator.HandleState(CurrentState, instant);
			}
		}
	}
}
