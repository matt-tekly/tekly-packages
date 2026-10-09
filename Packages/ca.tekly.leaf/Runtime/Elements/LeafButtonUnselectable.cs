using System;
using System.Collections;
using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Tekly.Leaf.Elements
{
	[ExecuteAlways]
	[SelectionBase]
	[DisallowMultipleComponent]
	public class LeafButtonUnselectable : UIBehaviour,
		IPointerDownHandler,
		IPointerUpHandler,
		IPointerEnterHandler,
		IPointerExitHandler,
		IPointerClickHandler,
		ILeafButton
	{
		/// <summary>
		/// This element can't be selected so IsSelected is always false.
		/// </summary>
		public LeafElementState CurrentState => m_tracker.GetState(IsInteractable(), IsOnState);

		public bool interactable {
			get => Interactable;
			set => Interactable = value;
		}

		public bool Interactable {
			get => m_interactable;
			set {
				if (m_interactable != value) {
					m_interactable = value;
					UpdateAnimatorState();
				}
			}
		}

		public SelectableSelectedEvent OnSelected => m_onSelected;
		public ButtonClickedEvent OnClicked => m_clicked;

		[SerializeField] private bool m_interactable = true;
		[SerializeField] protected LeafAnimator m_animator;

		[Tooltip("Add delay to when the press or submit is processed")] [SerializeField]
		private float m_pressDelay;

		[SerializeField] private ButtonClickedEvent m_clicked;

		private readonly LeafStateTracker m_tracker = new();
		private readonly SelectableSelectedEvent m_onSelected = new();
		
		private bool m_wasDeselectOnBackgroundClick;
		private bool m_isOverridingBackgroundClick;
		private bool m_groupsAllowInteraction = true;
		private bool m_isPressPending;
		private IDisposable m_disableInputScope;

		protected virtual bool IsOnState => false;

		protected override void OnEnable()
		{
			m_tracker.IsPressSimulated = false;
			m_groupsAllowInteraction = LeafCanvasGroups.AllowInteraction(transform);
			UpdateAnimatorState(CurrentState, true);
		}

		protected override void OnDisable()
		{
			// Pointer exit never arrives once disabled, e.g. when clicking this closes its panel
			RestoreDeselectOnBackgroundClick();

			// Disabling stops the press coroutine without running the rest of it, so release here
			m_isPressPending = false;
			ReleaseInput();

			m_tracker.Clear();

			UpdateAnimatorState(LeafElementState.Default.WithFlags(LeafElementFlags.On, IsOnState), true);
		}

		public virtual bool IsInteractable()
		{
			return m_groupsAllowInteraction && m_interactable;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			m_tracker.IsPointerInside = true;

			// While the pointer is inside this button we disable deselect on clicking on background elements.
			// This object isn't selectable so it would be considered a background element.
			if (!m_isOverridingBackgroundClick) {
				m_wasDeselectOnBackgroundClick = GetDeselectOnBackgroundClick();
				m_isOverridingBackgroundClick = true;
				SetDeselectOnBackgroundClick(false);
			}

			UpdateAnimatorState();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			m_tracker.IsPointerInside = false;

			RestoreDeselectOnBackgroundClick();
			UpdateAnimatorState();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left) {
				return;
			}

			m_tracker.IsPointerDown = true;
			UpdateAnimatorState();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left) {
				return;
			}

			m_tracker.IsPointerDown = false;
			UpdateAnimatorState();
		}

		public virtual void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left) {
				return;
			}

			if (m_pressDelay <= 0) {
				OnClick();
				UpdateAnimatorState();
			} else {
				StartPress(false);
			}
		}

		/// <summary>
		/// Shows the pressed state, then clicks after the press delay. Ignored while a press is already pending.
		/// </summary>
		public void SimulatePress()
		{
			StartPress(true);
		}

		protected override void OnCanvasGroupChanged()
		{
			RefreshGroupsAllowInteraction();
		}

		protected override void OnTransformParentChanged()
		{
			// Moving under a different CanvasGroup doesn't send OnCanvasGroupChanged
			RefreshGroupsAllowInteraction();
		}

		private void RefreshGroupsAllowInteraction()
		{
			var groupsAllowInteraction = LeafCanvasGroups.AllowInteraction(transform);

			if (groupsAllowInteraction != m_groupsAllowInteraction) {
				m_groupsAllowInteraction = groupsAllowInteraction;
				UpdateAnimatorState();
			}
		}

		protected virtual void OnClick()
		{
			if (!IsActive() || !IsInteractable()) {
				return;
			}

			m_clicked.Invoke();
		}

		protected virtual void UpdateAnimatorState(LeafElementState state, bool instant)
		{
			if (m_animator != null) {
				m_animator.HandleState(state, instant);
			}
		}

		protected void UpdateAnimatorState()
		{
			UpdateAnimatorState(CurrentState, false);
		}

		private void RestoreDeselectOnBackgroundClick()
		{
			if (!m_isOverridingBackgroundClick) {
				return;
			}

			m_isOverridingBackgroundClick = false;
			SetDeselectOnBackgroundClick(m_wasDeselectOnBackgroundClick);
		}

		private static void SetDeselectOnBackgroundClick(bool value)
		{
			// The EventSystem can already be gone when this is disabled during a scene unload
			if (EventSystem.current != null && EventSystem.current.currentInputModule is InputSystemUIInputModule module) {
				module.deselectOnBackgroundClick = value;
			}
		}

		private static bool GetDeselectOnBackgroundClick()
		{
			if (EventSystem.current != null && EventSystem.current.currentInputModule is InputSystemUIInputModule module) {
				return module.deselectOnBackgroundClick;
			}

			return false;
		}

		private void StartPress(bool showPressed)
		{
			if (!IsActive() || !IsInteractable() || m_isPressPending) {
				return;
			}

			m_isPressPending = true;

			if (showPressed) {
				m_tracker.IsPressSimulated = true;
				UpdateAnimatorState();
			}

			StartCoroutine(PressDelayCoroutine(m_pressDelay));
		}

		private IEnumerator PressDelayCoroutine(float delay)
		{
			m_disableInputScope = LeafCore.Instance.DisableInputScope(this);

			var elapsedTime = 0f;
			while (elapsedTime < delay) {
				elapsedTime += Time.unscaledDeltaTime;
				yield return null;
			}

			m_isPressPending = false;
			ReleaseInput();

			m_tracker.IsPressSimulated = false;
			UpdateAnimatorState();

			OnClick();
		}

		private void ReleaseInput()
		{
			m_disableInputScope?.Dispose();
			m_disableInputScope = null;
		}
	}
}
