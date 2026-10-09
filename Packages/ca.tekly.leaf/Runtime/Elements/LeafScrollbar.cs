using System.Collections;
using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Scrollbar driven by a <see cref="LeafAnimator"/> that ignores input while Leaf input is disabled
	/// (<see cref="LeafInputExtensions.IsLeafInputDisabled"/>). A press or drag in progress when input is disabled
	/// ends there, as if released, and one that starts while it's disabled is ignored until the next press, even if
	/// input comes back before release.
	/// </summary>
	public class LeafScrollbar : Scrollbar
	{
		public LeafElementState CurrentState => m_tracker.GetState(IsInteractable(), false);

		[SerializeField] private LeafAnimator m_animator;

		private readonly LeafStateTracker m_tracker = new();
		private Coroutine m_releaseWatch;

		// Drag events still arrive after an ignored or released press: the EventSystem picks the drag target on
		// press whether or not it was handled
		private bool m_isGestureCancelled;

		protected override void OnEnable()
		{
			m_tracker.IsPointerDown = false;
			m_tracker.IsPressSimulated = false;
			m_tracker.IsSelected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;

			base.OnEnable();
		}

		protected override void OnDisable()
		{
			// Disabling stops the coroutine
			m_releaseWatch = null;
			base.OnDisable();
		}

		protected override void InstantClearState()
		{
			m_tracker.Clear();
			base.InstantClearState();
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (this.IsLeafInputDisabled()) {
				m_isGestureCancelled = true;
				return;
			}

			m_isGestureCancelled = false;

			// Mirrors Scrollbar.MayDrag, which skips Selectable.OnPointerDown when it fails
			if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable()) {
				m_tracker.IsPointerDown = true;
			}

			base.OnPointerDown(eventData);

			if (m_tracker.IsPointerDown && m_releaseWatch == null) {
				m_releaseWatch = StartCoroutine(ReleaseWhenInputDisabled());
			}
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

		public override void OnBeginDrag(PointerEventData eventData)
		{
			if (IsGestureIgnored()) {
				return;
			}

			base.OnBeginDrag(eventData);
		}

		public override void OnDrag(PointerEventData eventData)
		{
			if (IsGestureIgnored()) {
				return;
			}

			base.OnDrag(eventData);
		}

		public override void OnMove(AxisEventData eventData)
		{
			// Moving along the axis changes the value, so this is checked here as well as in TryNavigateFrom
			if (this.IsLeafInputDisabled()) {
				return;
			}

			var isHorizontalMove = eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right;
			// Scrollbar.axis is private
			var isHorizontal = direction == Direction.LeftToRight || direction == Direction.RightToLeft;
			if (isHorizontalMove == isHorizontal) {
				base.OnMove(eventData);
			} else {
				LeafNavigationScope.TryNavigateFrom(this, eventData);
			}
		}

		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			if (m_animator == null) {
				base.DoStateTransition(state, instant);
			} else {
				m_animator.HandleState(CurrentState, instant);
			}
		}

		/// <summary>
		/// True when the current press was ignored or released early. Disabled input cancels the rest of the
		/// gesture here too, in case it lands before the release watch notices.
		/// </summary>
		private bool IsGestureIgnored()
		{
			if (this.IsLeafInputDisabled()) {
				m_isGestureCancelled = true;
			}

			return m_isGestureCancelled;
		}

		/// <summary>
		/// Runs while the pointer is held down. Holding on the track makes Scrollbar page towards the pointer every
		/// frame from its own coroutine, with no further events, so there's no handler to block it in. If input is
		/// disabled during the hold, this releases the press: Scrollbar's OnPointerUp is the only way to stop that
		/// coroutine, since ClickRepeat and the flag it loops on can't be overridden.
		/// </summary>
		private IEnumerator ReleaseWhenInputDisabled()
		{
			while (m_tracker.IsPointerDown) {
				if (this.IsLeafInputDisabled() && EventSystem.current != null) {
					m_isGestureCancelled = true;

					// Stops Scrollbar's click repeat, which pages towards the pointer every frame until release, and
					// clears the Pressed state
					OnPointerUp(new PointerEventData(EventSystem.current) {
						button = PointerEventData.InputButton.Left
					});
					break;
				}

				yield return null;
			}

			m_releaseWatch = null;
		}
	}
}
