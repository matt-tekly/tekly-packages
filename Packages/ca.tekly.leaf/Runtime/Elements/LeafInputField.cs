using Tekly.Leaf.Elements.Animators;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// TMP_InputField driven by a <see cref="LeafAnimator"/>. While the text is being edited the state
	/// has <see cref="LeafElementFlags.Focused"/> set, which replaces TMP's own "stay Selected while
	/// focused" transition.
	/// </summary>
	public class LeafInputField : TMP_InputField
	{
		public LeafElementState CurrentState => m_tracker.GetState(IsInteractable(), false)
			.WithFlags(LeafElementFlags.Focused, isFocused);

		[SerializeField] private LeafAnimator m_animator;

		private readonly LeafStateTracker m_tracker = new();
		private LeafNavigationElement m_leaf;
		private bool m_wasFocused;

		protected override void Awake()
		{
			base.Awake();
			m_leaf = GetComponent<LeafNavigationElement>();
		}

		protected override void OnEnable()
		{
			m_tracker.IsPointerDown = false;
			m_tracker.IsPressSimulated = false;
			m_tracker.IsSelected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;
			m_wasFocused = false;

			base.OnEnable();
		}

		protected override void InstantClearState()
		{
			m_tracker.Clear();
			base.InstantClearState();
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			// Mirrors TMP_InputField.MayDrag, which skips Selectable.OnPointerDown when it fails
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
			UpdateFocus();
		}

		public override void OnMove(AxisEventData eventData)
		{
			// Arrow keys move the caret while editing
			if (isFocused) {
				return;
			}

			if (m_leaf != null) {
				m_leaf.TryNavigate(eventData);
			}
		}

		protected override void LateUpdate()
		{
			// Focus changes without a state transition: activation is deferred to LateUpdate after
			// OnSelect, and Enter/Escape/clicking outside deactivate while the field stays selected.
			base.LateUpdate();
			UpdateFocus();
		}

		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			// Skips TMP's override on purpose, the Focused flag replaces its focus transition
			if (m_animator == null) {
				base.DoStateTransition(state, instant);
			} else {
				m_animator.HandleState(CurrentState, instant);
			}
		}

		private void UpdateFocus()
		{
			if (m_wasFocused == isFocused) {
				return;
			}

			m_wasFocused = isFocused;

			if (m_animator != null) {
				m_animator.HandleState(CurrentState, false);
			}
		}
	}
}
