using System;
using Tekly.Common.LifeCycles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Scrolling
{
	/// <summary>
	/// Scrolls the ScrollRect just enough to show a newly selected object inside its content, plus padding.
	/// Does nothing when the selection is already fully visible. Listens to <see cref="LeafSelection"/>, so it
	/// works the same for Tab, arrow keys, Enter and selection from code.
	///
	/// With a duration it glides there, measuring from where the content is already heading so quick or held
	/// arrow keys keep one smooth motion. Dragging or the scroll wheel cancels it. It only updates while moving.
	/// </summary>
	[RequireComponent(typeof(ScrollRect))]
	[DisallowMultipleComponent]
	public class LeafScrollToSelection : MonoBehaviour, IBeginDragHandler, IScrollHandler
	{
		[Tooltip("Space kept between the selection and the edge of the viewport")]
		[SerializeField] private float m_padding = 8f;

		[Tooltip("Roughly how long scrolling to a new selection takes, in unscaled seconds. 0 jumps straight there")]
		[SerializeField, Min(0f)] private float m_duration = 0.15f;

		private const float ARRIVED_DISTANCE = 0.1f;

		private ScrollRect m_scrollRect;
		private IDisposable m_selectionSubscription;

		private bool m_isAnimating;
		private Vector2 m_goal;
		private Vector2 m_velocity;
		private int m_enabledFrame;

		private static readonly Vector3[] s_corners = new Vector3[4];

		/// <summary>
		/// Scrolls the least distance that shows target fully. Something too big to fit is aligned to the top
		/// (or the left when scrolling horizontally). Animates over the duration unless instant.
		/// </summary>
		public void ScrollIntoView(RectTransform target, bool instant = false)
		{
			var content = m_scrollRect.content;
			if (content == null || target == null) {
				return;
			}

			var viewport = GetViewport();
			var view = viewport.rect;
			var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);

			// Measure from where the content is heading, so a second move while animating doesn't undershoot
			var pending = m_isAnimating ? ToViewport(viewport, m_goal - content.anchoredPosition) : Vector2.zero;
			bounds.center += (Vector3) pending;

			var delta = Vector2.zero;

			if (m_scrollRect.horizontal) {
				delta.x = GetDelta(bounds.min.x - m_padding, bounds.max.x + m_padding, view.xMin, view.xMax, false);
			}

			if (m_scrollRect.vertical) {
				delta.y = GetDelta(bounds.min.y - m_padding, bounds.max.y + m_padding, view.yMin, view.yMax, true);
			}

			if (delta == Vector2.zero && (!instant || !m_isAnimating)) {
				return;
			}

			m_scrollRect.StopMovement();

			var offset = ClampOffset(viewport, content, pending + delta);
			var goal = content.anchoredPosition + FromViewport(viewport, offset);

			if (instant || m_duration <= 0f) {
				StopAnimating();
				content.anchoredPosition = goal;
				return;
			}

			m_goal = goal;

			if (!m_isAnimating) {
				m_velocity = Vector2.zero;
				m_isAnimating = true;
				LifeCycle.Instance.LateUpdate += Tick;
			}
		}

		/// <summary>
		/// Stops a scroll in progress where it is.
		/// </summary>
		public void StopAnimating()
		{
			if (!m_isAnimating) {
				return;
			}

			m_isAnimating = false;
			LifeCycle.Instance.LateUpdate -= Tick;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			StopAnimating();
		}

		public void OnScroll(PointerEventData eventData)
		{
			StopAnimating();
		}

		private void Tick()
		{
			var content = m_scrollRect != null ? m_scrollRect.content : null;
			if (content == null) {
				StopAnimating();
				return;
			}

			var current = content.anchoredPosition;

			// The content can change size while scrolling (rows added or removed), so keep the goal in range
			var viewport = GetViewport();
			m_goal = current + FromViewport(viewport, ClampOffset(viewport, content, ToViewport(viewport, m_goal - current)));

			var next = Vector2.SmoothDamp(current, m_goal, ref m_velocity, m_duration, Mathf.Infinity, Time.unscaledDeltaTime);

			if ((next - m_goal).sqrMagnitude <= ARRIVED_DISTANCE * ARRIVED_DISTANCE) {
				next = m_goal;
				StopAnimating();
			}

			content.anchoredPosition = next;
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

				// The first selection after showing (e.g. a scope's entry) shouldn't scroll in from the top
				var instant = Time.frameCount - m_enabledFrame <= 1;
				ScrollIntoView(target, instant);
			}
		}

		private RectTransform GetViewport()
		{
			return m_scrollRect.viewport != null ? m_scrollRect.viewport : (RectTransform) transform;
		}

		/// <summary>
		/// Limits how far the content moves (in the viewport's space) so it stays within its ends, since padding
		/// can push past them and Elastic would spring back. An axis whose content fits in the view doesn't move.
		/// </summary>
		private Vector2 ClampOffset(RectTransform viewport, RectTransform content, Vector2 offset)
		{
			if (m_scrollRect.movementType == ScrollRect.MovementType.Unrestricted) {
				return offset;
			}

			var view = viewport.rect;

			// Only the content's own rect counts, as ScrollRect sees it, not its children
			content.GetWorldCorners(s_corners);
			var a = (Vector2) viewport.InverseTransformPoint(s_corners[0]);
			var b = (Vector2) viewport.InverseTransformPoint(s_corners[2]);
			var min = Vector2.Min(a, b);
			var max = Vector2.Max(a, b);

			offset.x = ClampAxis(offset.x, min.x, max.x, view.xMin, view.xMax);
			offset.y = ClampAxis(offset.y, min.y, max.y, view.yMin, view.yMax);
			return offset;
		}

		private static float ClampAxis(float offset, float contentMin, float contentMax, float viewMin, float viewMax)
		{
			if (contentMax - contentMin <= viewMax - viewMin) {
				return 0f;
			}

			return Mathf.Clamp(offset, viewMax - contentMax, viewMin - contentMin);
		}

		/// <summary>
		/// A movement of the content (in its parent's space, like anchoredPosition) seen in the viewport's space.
		/// </summary>
		private Vector2 ToViewport(RectTransform viewport, Vector2 contentMove)
		{
			var parent = m_scrollRect.content.parent;
			var world = parent != null ? parent.TransformVector(contentMove) : (Vector3) contentMove;
			return viewport.InverseTransformVector(world);
		}

		/// <summary>
		/// A movement in the viewport's space as a change to the content's anchoredPosition.
		/// </summary>
		private Vector2 FromViewport(RectTransform viewport, Vector2 viewportMove)
		{
			var world = viewport.TransformVector(viewportMove);
			var parent = m_scrollRect.content.parent;
			return parent != null ? parent.InverseTransformVector(world) : world;
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
			m_enabledFrame = Time.frameCount;
			m_selectionSubscription = LeafCore.Instance.Selection.Current.SubscribeChanges(OnSelectionChanged);
		}

		private void OnDisable()
		{
			StopAnimating();

			m_selectionSubscription?.Dispose();
			m_selectionSubscription = null;
		}
	}
}
