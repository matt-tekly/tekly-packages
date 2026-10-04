using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tekly.DevBoard.Pages
{
	/// <summary>
	/// Every page DevBoard knows about, built from the paths segments are registered under. A path with no
	/// segments of its own, like "Game" in "Game/Economy", becomes a folder page. Pages left with no segments
	/// and no children are removed, and so are their parents if that leaves them empty too.
	///
	/// Panels listen to the events to build, update and tear down their copies of pages. Changes made from
	/// inside an event handler (or a builder, or an OnDispose callback) are queued and applied once the
	/// current change has finished, so handlers always see a consistent tree.
	/// </summary>
	public class PageTree
	{
		public const string ROOT_TITLE = "DevBoard";

		public PageNode Root { get; }

		/// <summary>
		/// A segment was attached to a page. Its Node is set.
		/// </summary>
		public event Action<PageSegment> SegmentAdded;

		/// <summary>
		/// A segment was removed from the page passed with it.
		/// </summary>
		public event Action<PageSegment, PageNode> SegmentRemoved;

		/// <summary>
		/// A page was created. Its segments are added afterwards.
		/// </summary>
		public event Action<PageNode> PageAdded;

		/// <summary>
		/// A page was removed. Its descendants were removed before it.
		/// </summary>
		public event Action<PageNode> PageRemoved;

		/// <summary>
		/// A page gained or lost a child page.
		/// </summary>
		public event Action<PageNode> ChildrenChanged;

		private readonly Dictionary<string, PageNode> m_pages = new();
		private readonly Queue<Action> m_pending = new();

		private bool m_changing;
		private long m_nextSequence;

		public PageTree()
		{
			Root = new PageNode(string.Empty, ROOT_TITLE, null);
			m_pages.Add(Root.Path, Root);
		}

		/// <summary>
		/// Adds a segment to the page at path, creating the page and any missing parents.
		/// </summary>
		public PageSegment Add(string path, Action<PageContext> build, string title = null, int order = 0)
		{
			if (build == null) {
				throw new ArgumentNullException(nameof(build));
			}

			var segment = new PageSegment(this, NormalizePath(path), title, order, m_nextSequence++, build);
			Change(() => Attach(segment));

			return segment;
		}

		/// <summary>
		/// The page at path, or null.
		/// </summary>
		public PageNode Find(string path)
		{
			m_pages.TryGetValue(NormalizePath(path), out var page);
			return page;
		}

		/// <summary>
		/// The page at path, or its nearest existing ancestor. Never null: falls back to the root.
		/// </summary>
		public PageNode FindNearest(string path)
		{
			path = NormalizePath(path);

			while (true) {
				if (m_pages.TryGetValue(path, out var page)) {
					return page;
				}

				var slash = path.LastIndexOf('/');

				if (slash < 0) {
					return Root;
				}

				path = path.Substring(0, slash);
			}
		}

		/// <summary>
		/// Trims slashes and spaces: " Game//Economy/ " becomes "Game/Economy". Null becomes the root path.
		/// </summary>
		public static string NormalizePath(string path)
		{
			if (string.IsNullOrWhiteSpace(path)) {
				return string.Empty;
			}

			var builder = new StringBuilder(path.Length);

			foreach (var part in path.Split('/')) {
				var trimmed = part.Trim();

				if (trimmed.Length == 0) {
					continue;
				}

				if (builder.Length > 0) {
					builder.Append('/');
				}

				builder.Append(trimmed);
			}

			return builder.ToString();
		}

		internal void Remove(PageSegment segment)
		{
			Change(() => Detach(segment));
		}

		private void Change(Action change)
		{
			if (m_changing) {
				m_pending.Enqueue(change);
				return;
			}

			m_changing = true;

			try {
				RunSafely(change);

				while (m_pending.Count > 0) {
					RunSafely(m_pending.Dequeue());
				}
			} finally {
				m_changing = false;
			}
		}

		private void Attach(PageSegment segment)
		{
			// Disposed while the add was queued
			if (segment.IsDisposed) {
				return;
			}

			var page = GetOrCreate(segment.Path);
			segment.Node = page;
			page.AddSegment(segment);

			Raise(SegmentAdded, segment);
			UpdateOrders(page);
		}

		private void Detach(PageSegment segment)
		{
			var page = segment.Node;

			// Never attached: it was disposed before its queued add ran
			if (page == null) {
				return;
			}

			segment.Node = null;

			if (page.RemoveSegment(segment) < 0) {
				return;
			}

			Raise(SegmentRemoved, segment, page);
			UpdateOrders(Prune(page));
		}

		private PageNode GetOrCreate(string path)
		{
			if (m_pages.TryGetValue(path, out var page)) {
				return page;
			}

			var slash = path.LastIndexOf('/');
			var parent = slash < 0 ? Root : GetOrCreate(path.Substring(0, slash));
			var title = slash < 0 ? path : path.Substring(slash + 1);

			page = new PageNode(path, title, parent);
			m_pages.Add(path, page);
			parent.AddChild(page);

			Raise(PageAdded, page);
			Raise(ChildrenChanged, parent);

			return page;
		}

		/// <summary>
		/// Removes empty pages from page upwards. Returns the first page that's left.
		/// </summary>
		private PageNode Prune(PageNode page)
		{
			while (page != null && !page.IsRoot && page.IsEmpty) {
				var parent = page.Parent;

				parent.RemoveChild(page);
				m_pages.Remove(page.Path);
				page.IsRemoved = true;

				Raise(PageRemoved, page);
				Raise(ChildrenChanged, parent);

				page = parent;
			}

			return page;
		}

		/// <summary>
		/// A page's Order depends on its segments and children, so a change can move it, and every page above it,
		/// among their siblings. Re-sorts each level and tells listeners when a level's order changed.
		/// </summary>
		private void UpdateOrders(PageNode page)
		{
			for (var node = page; node != null && !node.IsRoot; node = node.Parent) {
				if (node.RefreshOrder()) {
					node.Parent.SortChildren();
					Raise(ChildrenChanged, node.Parent);
				}
			}
		}

		private static void RunSafely(Action action)
		{
			try {
				action();
			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}

		// Each handler runs on its own, so one panel throwing doesn't stop the others hearing about the change

		private static void Raise<T>(Action<T> handlers, T arg)
		{
			if (handlers == null) {
				return;
			}

			foreach (var handler in handlers.GetInvocationList()) {
				try {
					((Action<T>) handler)(arg);
				} catch (Exception exception) {
					Debug.LogException(exception);
				}
			}
		}

		private static void Raise<T1, T2>(Action<T1, T2> handlers, T1 arg1, T2 arg2)
		{
			if (handlers == null) {
				return;
			}

			foreach (var handler in handlers.GetInvocationList()) {
				try {
					((Action<T1, T2>) handler)(arg1, arg2);
				} catch (Exception exception) {
					Debug.LogException(exception);
				}
			}
		}
	}
}
