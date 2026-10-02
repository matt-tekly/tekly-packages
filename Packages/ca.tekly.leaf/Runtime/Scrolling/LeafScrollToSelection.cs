using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Leaf.Scrolling
{
	/// <summary>
	/// Scrolls the ScrollRect just enough to show a newly selected object inside its content, plus padding.
	/// Does nothing when the selection is already fully visible. Listens to <see cref="LeafSelection"/>, so it
	/// works the same for Tab, arrow keys, Enter and selection from code.
	/// </summary>
	[RequireComponent(typeof(ScrollRect))]
	[DisallowMultipleComponent]
	public class LeafScrollToSelection : MonoBehaviour
	{
		[Tooltip("Space kept between the selection and the edge of the viewport")]
		[SerializeField] private float m_padding = 8f;

		private ScrollRect m_scrollRect;
		private IDisposable m_selectionSubscription;

		/// <summary>
		/// Scrolls the least distance that shows target fully. Something too big to fit is aligned to the top
		/// (or the left when scrolling horizontally).
		/// </summary>
		public void ScrollIntoView(RectTransform target)
		{
			var content = m_scrollRect.content;
			if (content == null) {
				return;
			}

			var viewport = m_scrollRect.viewport != null ? m_scrollRect.viewport : (RectTransform) transform;
			var view = viewport.rect;
			var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
			var delta = Vector2.zero;

			if (m_scrollRect.horizontal) {
				delta.x = GetDelta(bounds.min.x - m_padding, bounds.max.x + m_padding, view.xMin, view.xMax, false);
			}

			if (m_scrollRect.vertical) {
				delta.y = GetDelta(bounds.min.y - m_padding, bounds.max.y + m_padding, view.yMin, view.yMax, true);
			}

			if (delta == Vector2.zero) {
				return;
			}

			m_scrollRect.StopMovement();

			// The delta is in the viewport's space, the content moves in its parent's
			var worldDelta = viewport.TransformVector(delta);
			var parent = content.parent;
			content.anchoredPosition += (Vector2) (parent != null ? parent.InverseTransformVector(worldDelta) : worldDelta);

			ClampToContent();
		}

		private void OnSelectionChanged(GameObject selected)
		{
			var content = m_scrollRect.content;
			if (selected == null || content == null || !selected.transform.IsChildOf(content)) {
				return;
			}

			if (selected.transform is RectTransform target) {
				// A widget added and selected this frame hasn't been laid out yet
				Canvas.ForceUpdateCanvases();
				ScrollIntoView(target);
			}
		}

		private void ClampToContent()
		{
			// Padding can push past the ends of the content, which Elastic would then spring back from
			if (m_scrollRect.movementType == ScrollRect.MovementType.Unrestricted) {
				return;
			}

			if (m_scrollRect.horizontal) {
				var x = m_scrollRect.horizontalNormalizedPosition;
				if (x < 0f || x > 1f) {
					m_scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(x);
				}
			}

			if (m_scrollRect.vertical) {
				var y = m_scrollRect.verticalNormalizedPosition;
				if (y < 0f || y > 1f) {
					m_scrollRect.verticalNormalizedPosition = Mathf.Clamp01(y);
				}
			}
		}

		private static float GetDelta(float min, float max, float viewMin, float viewMax, bool alignToMax)
		{
			if (max - min > viewMax - viewMin) {
				return alignToMax ? viewMax - max : viewMin - min;
			}

			if (max > viewMax) {
				return viewMax - max;
			}

			if (min < viewMin) {
				return viewMin - min;
			}

			return 0f;
		}

		private void Awake()
		{
			m_scrollRect = GetComponent<ScrollRect>();
		}

		private void OnEnable()
		{
			m_selectionSubscription = LeafCore.Instance.Selection.Current.SubscribeChanges(OnSelectionChanged);
		}

		private void OnDisable()
		{
			m_selectionSubscription?.Dispose();
			m_selectionSubscription = null;
		}
	}
}
