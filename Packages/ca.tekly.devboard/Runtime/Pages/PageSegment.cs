using System;
using Tekly.DevBoard.Nodes;

namespace Tekly.DevBoard.Pages
{
	/// <summary>
	/// A piece of content on a page, returned by DevBoard.AddPage. Several segments can share a path: they appear one
	/// after another on the same page. Dispose it to remove it, e.g. when the part of the game that registered it
	/// unloads; segments added with DevBoard.Page and an owner are removed with DevBoard.Release instead. Disposing
	/// more than once, or after DevBoard has shut down, does nothing.
	/// </summary>
	public sealed class PageSegment : IDisposable
	{
		/// <summary>
		/// What AddPage returns in builds without DevBoard: already removed, never built, and disposing it does
		/// nothing.
		/// </summary>
		public static PageSegment Inert { get; } = new(null, string.Empty, null, 0, 0, null) { IsDisposed = true };

		public string Path { get; }

		/// <summary>
		/// Shown above the segment's content. Null for no title.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Segments are sorted by Order, then by when they were registered.
		/// </summary>
		public int Order { get; }

		public bool IsDisposed { get; private set; }

		/// <summary>
		/// The page this segment is on, or null before it's attached or after it's removed.
		/// </summary>
		public PageNode Node { get; internal set; }

		/// <summary>
		/// The segment's content. Built by the builder the first time it's asked for, so pages nobody looks at cost
		/// nothing. Null once the segment is disposed.
		/// </summary>
		public Segment Root {
			get {
				if (m_root == null && !IsDisposed) {
					m_root = new Segment(this, m_build);
					m_root.Build();
				}

				return m_root;
			}
		}

		/// <summary>
		/// The content if it has been built, without building it.
		/// </summary>
		internal Segment BuiltRoot => m_root;

		internal long Sequence { get; }

		private readonly PageTree m_tree;
		private readonly Action<Segment> m_build;
		private Segment m_root;

		internal PageSegment(PageTree tree, string path, string title, int order, long sequence, Action<Segment> build)
		{
			m_tree = tree;
			Path = path;
			Title = string.IsNullOrEmpty(title) ? null : title;
			Order = order;
			Sequence = sequence;
			m_build = build;
		}

		public void Dispose()
		{
			if (IsDisposed) {
				return;
			}

			IsDisposed = true;
			m_tree?.Remove(this);
		}

		/// <summary>
		/// Runs the content's OnDispose callbacks. Called once the segment has left its page.
		/// </summary>
		internal void DisposeRoot()
		{
			var root = m_root;
			m_root = null;
			root?.DisposeTree();
		}
	}
}
