using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Leaf
{
	/// <summary>
	/// The cursor an element would like while the pointer is over it. Leaf only says which; the game draws it.
	/// </summary>
	public enum LeafCursor
	{
		/// No request: whatever the game shows by default
		None,

		/// Something to click, like a button
		Pointer,

		/// Text that can be typed in or selected
		Text,

		/// Something to drag
		Grab,

		/// Dragging left and right changes something, like a value scrubbed on a label
		ResizeHorizontal,

		/// Dragging up and down changes something
		ResizeVertical
	}

	/// <summary>
	/// The cursor Leaf elements are asking for, for the game to show however it draws cursors. Elements request
	/// one while the pointer is over them and they're interactable, and release it when it leaves.
	///
	/// Several requests can be active at once, e.g. a scrub label inside an input field: the highest priority
	/// wins, then the newest. Releasing one falls back to the next, so leaving the label goes back to the field's.
	/// Listen to <see cref="Changed"/>, or read <see cref="Current"/>. Anything else can request too, e.g. a world
	/// object under the pointer.
	/// </summary>
	public sealed class LeafCursorRequests
	{
		/// <summary>
		/// The cursor asked for now. None when nothing is asking.
		/// </summary>
		public LeafCursor Current { get; private set; }

		/// <summary>
		/// What's asking for <see cref="Current"/>, e.g. for debug tools. Null when nothing is.
		/// </summary>
		public object Owner { get; private set; }

		/// <summary>
		/// How many requests are active, the current one included.
		/// </summary>
		public int RequestCount => m_requests.Count;

		/// <summary>
		/// Raised with the new cursor when <see cref="Current"/> changes.
		/// </summary>
		public event Action<LeafCursor> Changed;

		private readonly List<Entry> m_requests = new List<Entry>(4);
		private int m_nextOrder;

		// LeafCore outlives play sessions when entering play mode doesn't reload the domain
		private static LeafCursorRequests s_instance;

		public LeafCursorRequests()
		{
			s_instance = this;
		}

		/// <summary>
		/// Forgets every request, e.g. ones left by elements from the last play session.
		/// </summary>
		public void Clear()
		{
			m_requests.Clear();
			Refresh();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearOnPlay()
		{
			s_instance?.Clear();
		}

		/// <summary>
		/// Asks for cursor on behalf of owner, replacing owner's earlier request. Higher priority wins over newer
		/// requests, e.g. 1 for a control inside another that asks for its own. None releases owner's request.
		/// </summary>
		public void Request(object owner, LeafCursor cursor, int priority = 0)
		{
			Remove(owner);

			if (cursor != LeafCursor.None) {
				m_requests.Add(new Entry(owner, cursor, priority, m_nextOrder++));
			}

			Refresh();
		}

		/// <summary>
		/// Withdraws owner's request, if it has one.
		/// </summary>
		public void Release(object owner)
		{
			if (Remove(owner)) {
				Refresh();
			}
		}

		private bool Remove(object owner)
		{
			for (var i = 0; i < m_requests.Count; i++) {
				if (m_requests[i].Owner == owner) {
					m_requests.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		private void Refresh()
		{
			var cursor = LeafCursor.None;
			object owner = null;
			var priority = int.MinValue;
			var order = int.MinValue;

			foreach (var request in m_requests) {
				if (request.Priority > priority || (request.Priority == priority && request.Order > order)) {
					cursor = request.Cursor;
					owner = request.Owner;
					priority = request.Priority;
					order = request.Order;
				}
			}

			Owner = owner;

			if (cursor == Current) {
				return;
			}

			Current = cursor;
			Changed?.Invoke(cursor);
		}

		private readonly struct Entry
		{
			public readonly object Owner;
			public readonly LeafCursor Cursor;
			public readonly int Priority;
			public readonly int Order;

			public Entry(object owner, LeafCursor cursor, int priority, int order)
			{
				Owner = owner;
				Cursor = cursor;
				Priority = priority;
				Order = order;
			}
		}
	}

	/// <summary>
	/// One element's cursor request, kept in step with whether it wants one. Elements call Update whenever their
	/// hover or interactable state changes; it only reaches <see cref="LeafCore"/> when the answer flips. It only
	/// requests in play mode, so editing prefabs doesn't create LeafCore, but always releases what it requested,
	/// even while leaving play mode, so no request outlives its element.
	/// </summary>
	public struct LeafCursorHover
	{
		private bool m_requested;

		/// <summary>
		/// Requests cursor for owner while wants is true, and releases it when it turns false.
		/// </summary>
		public void Update(object owner, bool wants, LeafCursor cursor, int priority = 0)
		{
			wants &= cursor != LeafCursor.None;

			if (wants == m_requested) {
				return;
			}

			if (!wants) {
				m_requested = false;
				LeafCore.Instance.Cursor.Release(owner);
				return;
			}

			if (Application.isPlaying) {
				m_requested = true;
				LeafCore.Instance.Cursor.Request(owner, cursor, priority);
			}
		}
	}
}
