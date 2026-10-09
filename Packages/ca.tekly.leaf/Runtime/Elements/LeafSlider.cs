using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafSlider : Slider
	{
		public LeafElementState CurrentState => m_tracker.GetState(IsInteractable(), false);

		[SerializeField] private LeafAnimator m_animator;

		private readonly LeafStateTracker m_tracker = new();

		protected override void OnEnable()
		{
			m_tracker.IsPointerDown = false;
			m_tracker.IsPressSimulated = false;
			m_tracker.IsSelected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;

			base.OnEnable();
		}

		protected override void InstantClearState()
		{
			m_tracker.Clear();
			base.InstantClearState();
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (this.IsLeafInputDisabled()) {
				return;
			}

			// Mirrors Slider.MayDrag, which skips Selectable.OnPointerDown when it fails
			if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable()) {
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
		}

		public override void OnDeselect(BaseEventData eventData)
		{
			m_tracker.IsSelected = false;
			base.OnDeselect(eventData);
		}

		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			if (m_animator == null) {
				base.DoStateTransition(state, instant);
			} else {
				m_animator.HandleState(CurrentState, instant);
			}
		}

		public override void OnDrag(PointerEventData eventData)
		{
			if (this.IsLeafInputDisabled()) {
				return;
			}

			base.OnDrag(eventData);
		}

		public override void OnMove(AxisEventData eventData)
		{
			// Left and Right change the value, so they're checked here as well as in TryNavigateFrom
			if (this.IsLeafInputDisabled()) {
				return;
			}

			// Along the slider's axis changes the value, across it navigates. Slider.axis is private
			var isHorizontalMove = eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right;
			var isHorizontal = direction == Direction.LeftToRight || direction == Direction.RightToLeft;
			if (isHorizontalMove == isHorizontal) {
				base.OnMove(eventData);
			} else {
				LeafNavigationScope.TryNavigateFrom(this, eventData);
			}
		}
	}
}
