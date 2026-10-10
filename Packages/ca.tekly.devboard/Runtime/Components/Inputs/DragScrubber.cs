using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.DevBoard.Components.Inputs
{
	/// <summary>
	/// Turns horizontal drags on an object (e.g. a number input's label) into value changes, like the labels in
	/// Unity's inspector. Mostly vertical drags are handed to the nearest ScrollRect above it, so the panel still
	/// scrolls. Not to the parent: that's usually the input field, which takes drags for selecting text.
	/// </summary>
	internal class DragScrubber : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		/// <summary>
		/// Called when a scrub starts, e.g. to stop the input field editing.
		/// </summary>
		public Action Began;

		/// <summary>
		/// Called with the horizontal distance dragged since the last call, in canvas units.
		/// </summary>
		public Action<float> Scrubbed;

		private bool m_scrubbing;

		public void OnInitializePotentialDrag(PointerEventData eventData)
		{
			// Lets a scroll view stop its own movement, as it would for a press on it
			Pass(eventData, ExecuteEvents.initializePotentialDrag);
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			m_scrubbing = false;

			if (Mathf.Abs(eventData.delta.x) < Mathf.Abs(eventData.delta.y)) {
				// The scroll view takes the rest of the drag
				eventData.pointerDrag = Pass(eventData, ExecuteEvents.beginDragHandler);
				return;
			}

			m_scrubbing = true;
			Began?.Invoke();
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (m_scrubbing) {
				Scrubbed?.Invoke(eventData.delta.x / CanvasScale());
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			m_scrubbing = false;
		}

		private GameObject Pass<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> function) where T : IEventSystemHandler
		{
			var parent = transform.parent;
			var scrollRect = parent != null ? parent.GetComponentInParent<ScrollRect>() : null;
			return scrollRect != null ? ExecuteEvents.ExecuteHierarchy(scrollRect.gameObject, eventData, function) : null;
		}

		private float CanvasScale()
		{
			var canvas = GetComponentInParent<Canvas>();
			return canvas != null ? Mathf.Max(0.0001f, canvas.rootCanvas.scaleFactor) : 1f;
		}
	}
}
