using System.Collections.Generic;
using Tekly.Common.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// Shows widgets only while the pointer is over this one, for nodes with RevealOnHover. Hidden widgets keep their
	/// space, so nothing shifts as they come and go. Added to a container only when one of its children needs it.
	///
	/// Touches have no hover: a tap shows the widgets until another HoverReveal is tapped.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class HoverReveal : DevBoardBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		// Unity objects compare equal to null once destroyed, so this needs no reset between sessions
		private static HoverReveal s_tapped;

		private readonly List<CanvasGroup> m_targets = new();
		private bool m_revealed;

		/// <summary>
		/// The HoverReveal on container, added the first time it's asked for.
		/// </summary>
		public static HoverReveal For(ContainerWidget container)
		{
			if (container.TryGetComponent(out HoverReveal reveal)) {
				return reveal;
			}

			// Hovering the gaps between widgets counts too, which needs something under the pointer there
			if (!container.TryGetComponent(out UnityEngine.UI.Graphic _)) {
				container.gameObject.AddComponent<BlockingGraphic>();
			}

			return container.gameObject.AddComponent<HoverReveal>();
		}

		/// <summary>
		/// Hides widget until the pointer is over this one.
		/// </summary>
		public void Add(Widget widget)
		{
			if (!widget.TryGetComponent(out CanvasGroup group)) {
				group = widget.gameObject.AddComponent<CanvasGroup>();
			}

			m_targets.Add(group);
			Apply(group, m_revealed);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (IsTouch(eventData)) {
				if (s_tapped != null && s_tapped != this) {
					s_tapped.SetRevealed(false);
				}

				s_tapped = this;
			}

			SetRevealed(true);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			// A finger lifting is an exit, but the tap was to show them
			if (!IsTouch(eventData)) {
				SetRevealed(false);
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			// No exit comes for a pointer that was over it when it went, e.g. a closed panel
			SetRevealed(false);
		}

		private void SetRevealed(bool revealed)
		{
			if (m_revealed == revealed) {
				return;
			}

			m_revealed = revealed;

			for (var i = m_targets.Count - 1; i >= 0; i--) {
				if (m_targets[i] == null) {
					m_targets.RemoveAt(i);
				} else {
					Apply(m_targets[i], revealed);
				}
			}
		}

		/// <summary>
		/// Only alpha and raycasts: interactable belongs to InteractableWhen.
		/// </summary>
		private static void Apply(CanvasGroup group, bool revealed)
		{
			group.alpha = revealed ? 1f : 0f;
			group.blocksRaycasts = revealed;
		}

		private static bool IsTouch(PointerEventData eventData)
		{
#if ENABLE_INPUT_SYSTEM
			if (eventData is ExtendedPointerEventData extended) {
				return extended.pointerType == UIPointerType.Touch;
			}
#endif
			// The old input module gives the mouse negative pointer ids, and touches count up from 0
			return eventData.pointerId >= 0;
		}
	}
}
