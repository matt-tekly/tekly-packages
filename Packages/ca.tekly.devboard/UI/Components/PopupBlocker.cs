using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// An invisible full-screen backdrop behind a popup. It swallows touches outside the popup, and a press on it
	/// dismisses the popup. Popups are built as children of it, so destroying it removes the popup too.
	/// </summary>
	internal class PopupBlocker : MonoBehaviour, IPointerDownHandler
	{
		public RectTransform RectTransform => (RectTransform) transform;

		private Action m_onDismiss;

		public static PopupBlocker Create(RectTransform layer, string name, Action onDismiss)
		{
			var gameObject = new GameObject(name, typeof(RectTransform));
			var rectTransform = (RectTransform) gameObject.transform;
			rectTransform.SetParent(layer, false);
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;

			// Clear, but still a raycast target so it catches presses
			var image = gameObject.AddComponent<Image>();
			image.color = Color.clear;

			var blocker = gameObject.AddComponent<PopupBlocker>();
			blocker.m_onDismiss = onDismiss;

			return blocker;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			// Presses inside the popup bubble up to here, since the popup is a child. Only dismiss on the backdrop
			if (eventData.pointerCurrentRaycast.gameObject == gameObject) {
				m_onDismiss?.Invoke();
			}
		}
	}
}
