using System.Collections.Generic;
using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.Events;
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
		IPointerExitHandler
	{
		public LeafElementMode CurrentMode {
			get {
				if (!IsInteractable()) {
					return LeafElementMode.Disabled;
				}

				if (m_isPointerDown) {
					return LeafElementMode.Pressed;
				}

				if (m_isPointerInside) {
					return LeafElementMode.Highlighted;
				}

				return LeafElementMode.Normal;
			}
		}

		public bool Interactable {
			get => m_interactable;
			set {
				if (m_interactable != value) {
					m_interactable = value;
					UpdateAnimatorMode();
				}
			}
		}

		[SerializeField] private bool m_interactable = true;
		[SerializeField] protected LeafAnimator m_animator;
		[SerializeField] private UnityEvent m_clicked;

		private bool m_isPointerInside;
		private bool m_isPointerDown;

		private bool m_wasDeselectOnBackgroundClick;
		private bool m_groupsAllowInteraction = true;

		private static readonly List<CanvasGroup> s_canvasGroupCache = new();


		protected override void OnEnable()
		{
			UpdateAnimatorMode(CurrentMode, true);
		}

		protected override void OnDisable()
		{
			m_isPointerInside = false;
			m_isPointerDown = false;

			UpdateAnimatorMode(LeafElementMode.Normal, true);
		}

		public bool IsInteractable()
		{
			return m_groupsAllowInteraction && m_interactable;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			m_isPointerInside = true;

			// While the pointer is inside this button we disable deselect on clicking on background elements.
			// This object isn't selectable so it would be considered a background element.
			m_wasDeselectOnBackgroundClick = GetDeselectOnBackgroundClick();
			SetDeselectOnBackgroundClick(false);

			UpdateAnimatorMode();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			m_isPointerInside = false;

			SetDeselectOnBackgroundClick(m_wasDeselectOnBackgroundClick);
			UpdateAnimatorMode();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			m_isPointerDown = true;
			UpdateAnimatorMode();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			var shouldClick = IsInteractable() && m_isPointerDown && m_isPointerInside;

			m_isPointerDown = false;
			if (shouldClick) {
				OnClick();
			}

			UpdateAnimatorMode();
		}

		protected override void OnCanvasGroupChanged()
		{
			var parentGroupAllowsInteraction = ParentGroupAllowsInteraction();

			if (parentGroupAllowsInteraction != m_groupsAllowInteraction) {
				m_groupsAllowInteraction = parentGroupAllowsInteraction;
				UpdateAnimatorMode();
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
			m_clicked.Invoke();
		}

		private void SetDeselectOnBackgroundClick(bool value)
		{
			if (EventSystem.current.currentInputModule is InputSystemUIInputModule module) {
				module.deselectOnBackgroundClick = value;
			}
		}

		protected virtual void UpdateAnimatorMode(LeafElementMode mode, bool instant)
		{
			if (m_animator != null) {
				m_animator.HandleMode(mode, false, instant);
			}
		}

		protected virtual void UpdateAnimatorMode()
		{
			UpdateAnimatorMode(CurrentMode, false);
		}

		private bool GetDeselectOnBackgroundClick()
		{
			if (EventSystem.current.currentInputModule is InputSystemUIInputModule module) {
				return module.deselectOnBackgroundClick;
			}

			return false;
		}
	}
}