using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafToggle : Toggle
	{
		public LeafElementState CurrentState => m_tracker.GetState(IsInteractable(), isOn);

		/// <summary>
		/// The cursor asked for while the pointer is over this and it's interactable. Takes effect the next time
		/// the pointer enters or the state changes.
		/// </summary>
		public LeafCursor Cursor {
			get => m_cursor;
			set => m_cursor = value;
		}

		[SerializeField] private LeafAnimator m_animator;

		[Tooltip("The cursor asked for while the pointer is over this and it's interactable. None asks for nothing")]
		[SerializeField] private LeafCursor m_cursor = LeafCursor.Pointer;

		private readonly LeafStateTracker m_tracker = new();
		private LeafCursorHover m_cursorHover;

		protected override void OnEnable()
		{
			m_tracker.IsPointerDown = false;
			m_tracker.IsPressSimulated = false;
			m_tracker.IsSelected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;

			// Toggle changes isOn without a state transition (and after pointer up on a click),
			// so the animator would keep the old On flag until the next pointer/selection change.
			// Hooked here rather than Awake: Awake isn't called again after a domain reload in the editor.
			onValueChanged.AddListener(OnToggleValueChanged);

			base.OnEnable();
		}

		protected override void OnDisable()
		{
			onValueChanged.RemoveListener(OnToggleValueChanged);
			base.OnDisable();
		}

		protected override void InstantClearState()
		{
			m_tracker.Clear();

			// Unity doesn't transition when it clears state on disable, so release the cursor here
			m_cursorHover.Update(this, false, m_cursor);
			base.InstantClearState();
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
		}

		public override void OnDeselect(BaseEventData eventData)
		{
			m_tracker.IsSelected = false;
			base.OnDeselect(eventData);
		}

		public override void OnPointerClick(PointerEventData eventData)
		{
			if (this.IsLeafInputDisabled()) {
				return;
			}

			base.OnPointerClick(eventData);
		}

		public override void OnSubmit(BaseEventData eventData)
		{
			if (this.IsLeafInputDisabled()) {
				return;
			}

			base.OnSubmit(eventData);
		}

		public override void OnMove(AxisEventData eventData)
		{
			LeafNavigationScope.TryNavigateFrom(this, eventData);
		}

		/// <summary>
		/// Hides Toggle.SetIsOnWithoutNotify, which changes isOn without raising onValueChanged, so the animator
		/// would keep showing the old On state until the next pointer or selection change. The new state is
		/// applied instantly: it's a programmatic sync, not something the user did.
		/// </summary>
		public new void SetIsOnWithoutNotify(bool value)
		{
			base.SetIsOnWithoutNotify(value);
			RefreshAnimator(true);
		}

		private void OnToggleValueChanged(bool value)
		{
			RefreshAnimator(false);
		}

		private void RefreshAnimator(bool instant)
		{
			if (m_animator != null) {
				m_animator.HandleState(CurrentState, instant);
			}
		}

		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			m_cursorHover.Update(this, m_tracker.HoldsCursor && IsInteractable(), m_cursor);

			if (m_animator == null) {
				base.DoStateTransition(state, instant);
			} else {
				m_animator.HandleState(CurrentState, instant);
			}
		}
	}
}
