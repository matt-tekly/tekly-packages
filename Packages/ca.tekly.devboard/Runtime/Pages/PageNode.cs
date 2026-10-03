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
		/// Child pages, sorted by title.
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
			var index = 0;

			while (index < m_children.Count && string.Compare(m_children[index].Title, child.Title, StringComparison.OrdinalIgnoreCase) <= 0) {
				index++;
			}

			m_children.Insert(index, child);
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

		private static int Compare(PageSegment a, PageSegment b)
		{
			var order = a.Order.CompareTo(b.Order);
			return order != 0 ? order : a.Sequence.CompareTo(b.Sequence);
		}
	}
}
