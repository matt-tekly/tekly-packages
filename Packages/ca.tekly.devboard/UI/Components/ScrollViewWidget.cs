using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// A scrolling container that remembers where it was scrolled to. The position is saved in DevBoard.State
	/// when the scroll view is hidden or destroyed, and restored after a rebuild once the content has grown back
	/// to the size it was when saved (or after a short timeout).
	/// </summary>
	public class ScrollViewWidget : ContainerWidget, IBeginDragHandler, IScrollHandler, ICanvasElement
	{
		/// <summary>
		/// How far the content is scrolled down from its top. Setting it jumps there, stopping any glide and any
		/// position waiting to be restored; it's clamped after the next layout.
		/// </summary>
		internal float VerticalOffset {
			get => m_scrollRect.content.anchoredPosition.y;
			set {
				m_restorePending = false;
				m_scrollRect.StopMovement();

				var content = m_scrollRect.content;
				content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Max(0f, value));
			}
		}

		/// <summary>
		/// The height of the area the content scrolls in. Zero until it's been laid out.
		/// </summary>
		internal float ViewportHeight => (m_scrollRect.viewport != null ? m_scrollRect.viewport : (RectTransform) m_scrollRect.transform).rect.height;

		private const string DEFAULT_STATE_KEY = "scroll";
		private const float RESTORE_TIMEOUT = 1f;

		[SerializeField] private ScrollRect m_scrollRect;

		private string m_stateKey;
		private bool m_saveState = true;
		private bool m_restorePending;
		private ScrollState m_pendingState;
		private float m_restoreDeadline;
		private bool m_scrollbarsFrozen;
		private Scrollbar m_frozenHorizontal;
		private Scrollbar m_frozenVertical;

		private static readonly Vector3[] s_corners = new Vector3[4];
		/// <summary>
		/// Saves the scroll position under key instead of the default. Use it when one container holds two scroll views.
		/// </summary>
		public ScrollViewWidget WithStateKey(string key)
		{
			m_stateKey = WidgetState.KeyFor(this, key);
			return this;
		}

		/// <summary>
		/// Turns off saving the position in DevBoard.State, for scroll views whose owner tracks the position itself.
		/// </summary>
		public ScrollViewWidget WithoutSavedState()
		{
			m_saveState = false;
			return this;
		}

		/// <summary>
		/// Holds the scrollbars as they are, e.g. while the scroll view's container eases to a new size and the
		/// content briefly doesn't fit. A scrollbar that's showing stays, so it doesn't flicker, and a hidden one
		/// doesn't pop in and narrow the view. Unfreezing hands them back to the ScrollRect, which shows them as
		/// the content needs.
		/// </summary>
		internal void FreezeScrollbars(bool frozen)
		{
			if (frozen == m_scrollbarsFrozen || m_scrollRect == null) {
				return;
			}

			m_scrollbarsFrozen = frozen;

			if (frozen) {
				m_frozenHorizontal = m_scrollRect.horizontalScrollbar;
				m_frozenVertical = m_scrollRect.verticalScrollbar;
				m_scrollRect.horizontalScrollbar = null;
				m_scrollRect.verticalScrollbar = null;
			} else {
				m_scrollRect.horizontalScrollbar = m_frozenHorizontal;
				m_scrollRect.verticalScrollbar = m_frozenVertical;
				m_frozenHorizontal = null;
				m_frozenVertical = null;
			}
		}

		/// <summary>
		/// The current position, to hand back to RestorePosition later.
		/// </summary>
		internal ScrollState CapturePosition()
		{
			if (m_restorePending) {
				return m_pendingState;
			}

			var content = m_scrollRect.content;

			return new ScrollState {
				Position = content.anchoredPosition,
				ContentSize = content.rect.size
			};
		}

		/// <summary>
		/// Scrolls to a captured position once the content has been laid out at its captured size.
		/// The default ScrollState scrolls to the start straight away.
		/// </summary>
		internal void RestorePosition(ScrollState state)
		{
			m_pendingState = state;
			m_restorePending = true;
			m_restoreDeadline = Time.realtimeSinceStartup + RESTORE_TIMEOUT;
		}

		private void Awake()
		{
			if (m_scrollRect == null) {
				m_scrollRect = GetComponent<ScrollRect>();
			}
		}

		private void Start()
		{
			if (!m_saveState) {
				return;
			}

			// Start runs after the code building the board, so the key reflects where the scroll view ended up
			m_stateKey ??= WidgetState.KeyFor(this, DEFAULT_STATE_KEY);

			var state = DevBoard.Instance?.State;

			if (state != null && state.TryGet(m_stateKey, out ScrollState saved)) {
				m_pendingState = saved;
				m_restorePending = true;
				m_restoreDeadline = Time.realtimeSinceStartup + RESTORE_TIMEOUT;
			}
		}

		private void LateUpdate()
		{
			// The layout queue empties every frame, so join it again to clamp after this frame's layout
			CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);

			if (!m_restorePending || m_scrollRect == null) {
				return;
			}

			// Restoring before layout has sized the content would just be clamped back to the top
			var content = m_scrollRect.content;
			var size = content.rect.size;
			var target = m_pendingState.ContentSize;
			var isLaidOut = size.x >= target.x - 1f && size.y >= target.y - 1f;

			if (!isLaidOut && Time.realtimeSinceStartup < m_restoreDeadline) {
				return;
			}

			m_scrollRect.StopMovement();
			content.anchoredPosition = m_pendingState.Position;
			m_restorePending = false;
		}

		void ICanvasElement.Rebuild(CanvasUpdate executing)
		{
			if (executing == CanvasUpdate.PostLayout) {
				ClampAfterLayout();
			}
		}

		void ICanvasElement.LayoutComplete()
		{
		}

		void ICanvasElement.GraphicUpdateComplete()
		{
		}

		bool ICanvasElement.IsDestroyed()
		{
			return this == null;
		}

		/// <summary>
		/// Scrolls just enough to show target, with margin around it, if it's somewhere in the content. Something
		/// taller than the view is lined up with its top. Call it once layout has placed target; it replaces any
		/// position waiting to be restored.
		/// </summary>
		public void ScrollIntoView(RectTransform target, float margin = 8f)
		{
			var content = m_scrollRect != null ? m_scrollRect.content : null;

			if (content == null || target == null || !target.IsChildOf(content)) {
				return;
			}

			m_restorePending = false;
			m_scrollRect.StopMovement();

			var viewport = m_scrollRect.viewport != null ? m_scrollRect.viewport : (RectTransform) m_scrollRect.transform;
			var view = viewport.rect;

			target.GetWorldCorners(s_corners);
			var a = (Vector2) viewport.InverseTransformPoint(s_corners[0]);
			var b = (Vector2) viewport.InverseTransformPoint(s_corners[2]);
			var min = Vector2.Min(a, b);
			var max = Vector2.Max(a, b);

			var offset = Vector2.zero;

			if (m_scrollRect.horizontal) {
				offset.x = RevealAxis(min.x, max.x, view.xMin, view.xMax, margin, false);
			}

			if (m_scrollRect.vertical) {
				offset.y = RevealAxis(min.y, max.y, view.yMin, view.yMax, margin, true);
			}

			if (offset.sqrMagnitude < 0.0001f) {
				return;
			}

			var world = viewport.TransformVector(offset);
			var parent = content.parent;
			content.anchoredPosition += (Vector2) (parent != null ? parent.InverseTransformVector(world) : world);
		}

		/// <summary>
		/// How far the content must move along one axis to bring min..max into the view. The start of the axis is
		/// the top (max) vertically and the left (min) horizontally; something too big to fit is lined up with it.
		/// </summary>
		private static float RevealAxis(float min, float max, float viewMin, float viewMax, float margin, bool startIsMax)
		{
			var fits = max - min <= viewMax - viewMin - margin * 2f;

			if (!fits) {
				return startIsMax ? viewMax - margin - max : viewMin + margin - min;
			}

			if (max + margin > viewMax) {
				return viewMax - margin - max;
			}

			if (min - margin < viewMin) {
				return viewMin + margin - min;
			}

			return 0f;
		}

		/// <summary>
		/// ScrollRect only clamps in LateUpdate, before the canvas lays out. When the content shrinks while
		/// scrolled to the end (like a foldout collapsing) that frame would draw out of range and snap back the
		/// next, so clamp after layout. It has to be before RectMask2D culls, or rows moving into view stay hidden
		/// for a frame. Only for Clamped, since Elastic is meant to overshoot while dragging.
		/// </summary>
		private void ClampAfterLayout()
		{
			if (m_scrollRect == null || m_restorePending || m_scrollRect.movementType != ScrollRect.MovementType.Clamped) {
				return;
			}

			var content = m_scrollRect.content;
			var viewport = m_scrollRect.viewport != null ? m_scrollRect.viewport : (RectTransform) m_scrollRect.transform;

			if (content == null) {
				return;
			}

			var view = viewport.rect;
			content.GetWorldCorners(s_corners);
			var a = (Vector2) viewport.InverseTransformPoint(s_corners[0]);
			var b = (Vector2) viewport.InverseTransformPoint(s_corners[2]);
			var min = Vector2.Min(a, b);
			var max = Vector2.Max(a, b);

			var offset = Vector2.zero;

			if (m_scrollRect.horizontal) {
				offset.x = ClampAxis(min.x, max.x, view.xMin, view.xMax, content.pivot.x);
			}

			if (m_scrollRect.vertical) {
				offset.y = ClampAxis(min.y, max.y, view.yMin, view.yMax, content.pivot.y);
			}

			if (offset.sqrMagnitude < 0.0001f) {
				return;
			}

			var world = viewport.TransformVector(offset);
			var parent = content.parent;
			content.anchoredPosition += (Vector2) (parent != null ? parent.InverseTransformVector(world) : world);

			// Its own PostLayout pass may already have run this frame, so bring the scrollbars up to date now
			m_scrollRect.Rebuild(CanvasUpdate.PostLayout);
		}

		/// <summary>
		/// How far the content must move to sit within the view the way ScrollRect clamps it. Content that fits
		/// is placed by its pivot, as ScrollRect does.
		/// </summary>
		private static float ClampAxis(float contentMin, float contentMax, float viewMin, float viewMax, float pivot)
		{
			var contentSize = contentMax - contentMin;
			var viewSize = viewMax - viewMin;

			if (contentSize <= viewSize) {
				return viewMin + (viewSize - contentSize) * pivot - contentMin;
			}

			if (contentMin > viewMin) {
				return viewMin - contentMin;
			}

			if (contentMax < viewMax) {
				return viewMax - contentMax;
			}

			return 0f;
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			// If the restore never happened, keep the earlier saved position rather than saving the unrestored one
			if (m_saveState && !m_restorePending) {
				Save();
			}
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			// The user is scrolling, so don't jump them somewhere else
			m_restorePending = false;
		}

		public void OnScroll(PointerEventData eventData)
		{
			m_restorePending = false;
		}

		private void Save()
		{
			var state = DevBoard.Instance?.State;

			if (state == null || m_stateKey == null || m_scrollRect == null) {
				return;
			}

			var content = m_scrollRect.content;

			state.Set(m_stateKey, new ScrollState {
				Position = content.anchoredPosition,
				ContentSize = content.rect.size
			});
		}

		internal struct ScrollState
		{
			public Vector2 Position;
			public Vector2 ContentSize;
		}
	}

	public static class ScrollViewWidgetFactory
	{
		public static ScrollViewWidget ScrollView(this ContainerWidget parent, WidgetPrefab<ScrollViewWidget> prefab = default)
		{
			return parent.Create(prefab.Or(WidgetPrefabs.ScrollView));
		}
	}
}
