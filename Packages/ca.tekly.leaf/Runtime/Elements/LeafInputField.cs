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
	public class LeafInputField : TMP_InputField, ILeafTabHandler
	{
		public LeafElementState CurrentState => m_tracker.GetState(IsInteractable(), false)
			.WithFlags(LeafElementFlags.Focused, isFocused);

		public bool TabNavigates {
			get => m_tabNavigates;
			set => m_tabNavigates = value;
		}

		public bool EnterMovesNext {
			get => m_enterMovesNext;
			set => m_enterMovesNext = value;
		}

		[SerializeField] private LeafAnimator m_animator;

		[Tooltip("Tab moves to the next element instead of typing a tab character. Only matters for multi-line fields: single-line fields never take tabs.")]
		[SerializeField] private bool m_tabNavigates = true;

		[Tooltip("Enter ends the edit and selects the next element in tab order, like Tab. Leave it off on a form's last field and use On Submit instead. Ignored when Enter types a new line.")]
		[SerializeField] private bool m_enterMovesNext;

		private readonly LeafStateTracker m_tracker = new();
		private bool m_wasFocused;
		private bool m_isMoveNextPending;
		private int m_moveNextFrame;

		protected override void OnEnable()
		{
			m_tracker.IsPointerDown = false;
			m_tracker.IsPressSimulated = false;
			m_tracker.IsSelected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;
			m_wasFocused = false;
			m_isMoveNextPending = false;

			// Hooked here rather than Awake: Awake isn't called again after a domain reload in the editor
			onSubmit.AddListener(OnSubmitted);

			base.OnEnable();
		}

		protected override void OnDisable()
		{
			onSubmit.RemoveListener(OnSubmitted);
			m_isMoveNextPending = false;

			base.OnDisable();
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

			LeafNavigationScope.TryNavigateFrom(this, eventData);
		}

		public override void OnSubmit(BaseEventData eventData)
		{
			// The Enter that ended the edit can also arrive as a Submit event, which would start editing again
			if (m_isMoveNextPending) {
				eventData?.Use();
				return;
			}

			base.OnSubmit(eventData);
		}

		public void OnTab(LeafTabEventData eventData)
		{
			// A multi-line field that types tabs keeps Tab while editing
			if (isFocused && multiLine && !m_tabNavigates) {
				eventData.Use();
			}
		}

		protected override void Append(char input)
		{
			// TMP can read the key before the scope moves the selection away this frame, so a typed tab is
			// dropped here as well. Checking the key keeps tabs in pasted text.
			if (input == '\t' && m_tabNavigates && LeafTabInput.WasPressedThisFrame(out _)) {
				return;
			}

			base.Append(input);
		}

		protected override void LateUpdate()
		{
			// Focus changes without a state transition: activation is deferred to LateUpdate after
			// OnSelect, and Enter/Escape/clicking outside deactivate while the field stays selected.
			base.LateUpdate();
			UpdateFocus();

			// Moving a frame after Enter, so the same key press can't also submit the next element,
			// e.g. press a button that follows the field
			if (m_isMoveNextPending && Time.frameCount > m_moveNextFrame) {
				m_isMoveNextPending = false;

				var eventSystem = EventSystem.current;
				if (eventSystem != null && eventSystem.currentSelectedGameObject == gameObject) {
					LeafNavigationScope.TrySelectNextFrom(this, false);
				}
			}
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

		private void OnSubmitted(string text)
		{
			// Only when Enter ends an edit: Submit on a field that isn't being edited starts editing instead
			if (!m_enterMovesNext || !isFocused || lineType == LineType.MultiLineNewline) {
				return;
			}

			m_isMoveNextPending = true;
			m_moveNextFrame = Time.frameCount;
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
