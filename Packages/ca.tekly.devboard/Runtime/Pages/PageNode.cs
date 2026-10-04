using System;
using System.Collections.Generic;

namespace Tekly.DevBoard.Pages
{
	/// <summary>
	/// One page in the <see cref="PageTree"/>. A page has segments (content registered for its path) and child
	/// pages. It holds no UI: each panel builds its own copy of a page from the segments' builders.
	/// </summary>
	public class PageNode
	{
		/// <summary>
		/// Full path, e.g. "Game/Economy". The root page's path is empty.
		/// </summary>
		public string Path { get; }

		/// <summary>
		/// The last part of the path, e.g. "Economy".
		/// </summary>
		public string Title { get; }

		public PageNode Parent { get; }
		public bool IsRoot => Parent == null;

		/// <summary>
		/// Where this page sorts among its siblings: the lowest Order of its segments, or for a folder page with
		/// no segments of its own, the lowest Order of its children. 0 when it has neither.
		/// </summary>
		public int Order { get; private set; }

		/// <summary>
		/// Child pages, sorted by Order and then by title.
		/// </summary>
		public IReadOnlyList<PageNode> Children => m_children;

		/// <summary>
		/// Segments, sorted by order and then by when they were registered.
		/// </summary>
		public IReadOnlyList<PageSegment> Segments => m_segments;

		/// <summary>
		/// True once the page has been removed from the tree because it had no segments or children left.
		/// </summary>
		public bool IsRemoved { get; internal set; }

		internal bool IsEmpty => m_segments.Count == 0 && m_children.Count == 0;

		private readonly List<PageNode> m_children = new();
		private readonly List<PageSegment> m_segments = new();

		internal PageNode(string path, string title, PageNode parent)
		{
			Path = path;
			Title = title;
			Parent = parent;
		}

		/// <summary>
		/// True if this page is other or one of its descendants.
		/// </summary>
		public bool IsSelfOrDescendantOf(PageNode other)
		{
			for (var node = this; node != null; node = node.Parent) {
				if (node == other) {
					return true;
				}
			}

			return false;
		}

		internal void AddChild(PageNode child)
		{
			m_children.Add(child);
			SortChildren();
		}

		internal void SortChildren()
		{
			m_children.Sort(CompareChildren);
		}

		/// <summary>
		/// Recalculates Order from the segments and children. Returns true if it changed.
		/// </summary>
		internal bool RefreshOrder()
		{
			var order = 0;

			if (m_segments.Count > 0) {
				order = int.MaxValue;

				foreach (var segment in m_segments) {
					order = Math.Min(order, segment.Order);
				}
			} else if (m_children.Count > 0) {
				order = int.MaxValue;

				foreach (var child in m_children) {
					order = Math.Min(order, child.Order);
				}
			}

			if (order == Order) {
				return false;
			}

			Order = order;
			return true;
		}

		internal void RemoveChild(PageNode child)
		{
			m_children.Remove(child);
		}

		/// <summary>
		/// Inserts the segment in order and returns its index.
		/// </summary>
		internal int AddSegment(PageSegment segment)
		{
			var index = 0;

			while (index < m_segments.Count && Compare(m_segments[index], segment) <= 0) {
				index++;
			}

			m_segments.Insert(index, segment);
			return index;
		}

		/// <summary>
		/// Removes the segment and returns the index it had, or -1 if it wasn't on this page.
		/// </summary>
		internal int RemoveSegment(PageSegment segment)
		{
			var index = m_segments.IndexOf(segment);

			if (index >= 0) {
				m_segments.RemoveAt(index);
			}

			return index;
		}

		private static int CompareChildren(PageNode a, PageNode b)
		{
			var order = a.Order.CompareTo(b.Order);
			return order != 0 ? order : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
		}

		private static int Compare(PageSegment a, PageSegment b)
		{
			var order = a.Order.CompareTo(b.Order);
			return order != 0 ? order : a.Sequence.CompareTo(b.Sequence);
		}
	}
}
