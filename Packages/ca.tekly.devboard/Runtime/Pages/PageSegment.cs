using System;
using UnityEngine;

namespace Tekly.DevBoard.Pages
{
	/// <summary>
	/// A piece of content on a page, returned by DevBoard.Page. Several segments can share a path: they appear one
	/// after another on the same page. Dispose it to remove it, e.g. when the part of the game that registered it
	/// unloads. Disposing more than once, or after DevBoard has shut down, does nothing.
	/// </summary>
	public sealed class PageSegment : IDisposable
	{
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

		internal long Sequence { get; }
		internal Action<PageContext> Build { get; }

		private readonly PageTree m_tree;

		internal PageSegment(PageTree tree, string path, string title, int order, long sequence, Action<PageContext> build)
		{
			m_tree = tree;
			Path = path;
			Title = string.IsNullOrEmpty(title) ? null : title;
			Order = order;
			Sequence = sequence;
			Build = build;
		}

		public void Dispose()
		{
			if (IsDisposed) {
				return;
			}

			IsDisposed = true;
			m_tree.Remove(this);
		}

		/// <summary>
		/// Disposes this segment when owner is destroyed.
		/// </summary>
		public PageSegment BindTo(GameObject owner)
		{
			if (owner == null) {
				Dispose();
				return this;
			}

			if (!owner.TryGetComponent(out PageSegmentBinding binding)) {
				binding = owner.AddComponent<PageSegmentBinding>();
			}

			binding.Add(this);
			return this;
		}
	}
}
