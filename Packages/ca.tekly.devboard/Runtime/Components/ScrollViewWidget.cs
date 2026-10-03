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
	public class ScrollViewWidget : ContainerWidget, IBeginDragHandler, IScrollHandler
	{
		private const string DEFAULT_STATE_KEY = "scroll";
		private const float RESTORE_TIMEOUT = 1f;

		[SerializeField] private ScrollRect m_scrollRect;

		private string m_stateKey;
		private bool m_restorePending;
		private ScrollState m_pendingState;
		private float m_restoreDeadline;

		/// <summary>
		/// Saves the scroll position under key instead of the default. Use it when one container holds two scroll views.
		/// </summary>
		public ScrollViewWidget WithStateKey(string key)
		{
			m_stateKey = DevBoardState.KeyFor(this, key);
			return this;
		}

		private void Awake()
		{
			if (m_scrollRect == null) {
				m_scrollRect = GetComponent<ScrollRect>();
			}
		}

		private void Start()
		{
			// Start runs after the code building the board, so the key reflects where the scroll view ended up
			m_stateKey ??= DevBoardState.KeyFor(this, DEFAULT_STATE_KEY);

			var state = DevBoard.Instance?.State;

			if (state != null && state.TryGet(m_stateKey, out ScrollState saved)) {
				m_pendingState = saved;
				m_restorePending = true;
				m_restoreDeadline = Time.realtimeSinceStartup + RESTORE_TIMEOUT;
			}
		}

		private void LateUpdate()
		{
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

		protected override void OnDisable()
		{
			base.OnDisable();

			// If the restore never happened, keep the earlier saved position rather than saving the unrestored one
			if (!m_restorePending) {
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

		private struct ScrollState
		{
			public Vector2 Position;
			public Vector2 ContentSize;
		}
	}
}
