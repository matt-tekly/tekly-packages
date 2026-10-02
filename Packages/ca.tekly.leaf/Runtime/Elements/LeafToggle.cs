using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafToggle : Toggle
	{
		public LeafElementState CurrentState => m_tracker.GetState(IsInteractable(), isOn);

		[SerializeField] private LeafAnimator m_animator;

		private readonly LeafStateTracker m_tracker = new();

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

		public override void OnMove(AxisEventData eventData)
		{
			LeafNavigationScope.TryNavigateFrom(this, eventData);
		}

		private void OnToggleValueChanged(bool value)
		{
			if (m_animator != null) {
				m_animator.HandleState(CurrentState, false);
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
	}
}
