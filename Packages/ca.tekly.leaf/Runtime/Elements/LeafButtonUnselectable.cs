using System.Collections;
using System.Collections.Generic;
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
		private bool m_groupsAllowInteraction = true;

		private static readonly List<CanvasGroup> s_canvasGroupCache = new();

		protected virtual bool IsOnState => false;

		protected override void OnEnable()
		{
			m_tracker.IsPressSimulated = false;
			m_groupsAllowInteraction = ParentGroupAllowsInteraction();
			UpdateAnimatorState(CurrentState, true);
		}

		protected override void OnDisable()
		{
			m_tracker.Clear();

			UpdateAnimatorState(LeafElementState.Default.WithFlags(LeafElementFlags.On, IsOnState), true);
		}

		public bool IsInteractable()
		{
			return m_groupsAllowInteraction && m_interactable;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			m_tracker.IsPointerInside = true;

			// While the pointer is inside this button we disable deselect on clicking on background elements.
			// This object isn't selectable so it would be considered a background element.
			m_wasDeselectOnBackgroundClick = GetDeselectOnBackgroundClick();
			SetDeselectOnBackgroundClick(false);

			UpdateAnimatorState();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			m_tracker.IsPointerInside = false;

			SetDeselectOnBackgroundClick(m_wasDeselectOnBackgroundClick);
			UpdateAnimatorState();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			m_tracker.IsPointerDown = true;
			UpdateAnimatorState();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			m_tracker.IsPointerDown = false;
			UpdateAnimatorState();
		}

		public virtual void OnPointerClick(PointerEventData eventData)
		{
			if (m_pressDelay <= 0) {
				OnClick();
				UpdateAnimatorState();
			} else {
				StartCoroutine(PressDelayCoroutine(m_pressDelay));
			}
		}

		public void SimulatePress()
		{
			m_tracker.IsPressSimulated = true;
			UpdateAnimatorState();
			StartCoroutine(PressDelayCoroutine(m_pressDelay));
		}

		protected override void OnCanvasGroupChanged()
		{
			var parentGroupAllowsInteraction = ParentGroupAllowsInteraction();

			if (parentGroupAllowsInteraction != m_groupsAllowInteraction) {
				m_groupsAllowInteraction = parentGroupAllowsInteraction;
				UpdateAnimatorState();
			}
		}

		private bool ParentGroupAllowsInteraction()
		{
			var t = transform;
			while (t != null) {
				t.GetComponents(s_canvasGroupCache);
				foreach (var canvasGroup in s_canvasGroupCache) {
					if (canvasGroup.enabled && !canvasGroup.interactable) {
						return false;
					}

					if (canvasGroup.ignoreParentGroups) {
						return true;
					}
				}

				t = t.parent;
			}

			return true;
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

		private static void SetDeselectOnBackgroundClick(bool value)
		{
			if (EventSystem.current.currentInputModule is InputSystemUIInputModule module) {
				module.deselectOnBackgroundClick = value;
			}
		}

		private static bool GetDeselectOnBackgroundClick()
		{
			if (EventSystem.current.currentInputModule is InputSystemUIInputModule module) {
				return module.deselectOnBackgroundClick;
			}

			return false;
		}

		private IEnumerator PressDelayCoroutine(float delay)
		{
			using (LeafCore.Instance.DisableInputScope(this)) {
				var fadeTime = delay;
				var elapsedTime = 0f;

				while (elapsedTime < fadeTime) {
					elapsedTime += Time.unscaledDeltaTime;
					yield return null;
				}
			}

			m_tracker.IsPressSimulated = false;
			UpdateAnimatorState();

			OnClick();
		}
	}
}
