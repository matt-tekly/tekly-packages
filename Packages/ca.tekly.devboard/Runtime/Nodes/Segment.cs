using System;
using Tekly.DevBoard.Pages;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// The content of a page segment, passed to the builder given to DevBoard.Page. The builder runs once, the
	/// first time something needs the segment, and every panel showing the page renders the same nodes. So state
	/// can live in the builder, and a Rebuild shows up in every panel.
	/// </summary>
	public sealed class Segment : ScopeNode
	{
		public PageSegment Source { get; }

		/// <summary>
		/// Shown above the content. Null for no title.
		/// </summary>
		public string Title => Source.Title;

		public override string Path => m_path ??= string.IsNullOrEmpty(Source.Path) ? Id : $"{NodePaths.SlugPath(Source.Path)}/{Id}";

		/// <summary>
		/// Always a level, titled or not: a page's segments are told apart by it.
		/// </summary>
		internal override bool AddsPathLevel => true;

		private readonly Action<Segment> m_build;
		private string m_path;

		internal Segment(PageSegment source, Action<Segment> build) : base(source.Title, null)
		{
			Source = source;
			m_build = build;
			Id = NodePaths.Slug(source.Title) ?? "segment";
		}

		/// <summary>
		/// The segment node is in, or null if it isn't in one.
		/// </summary>
		public static Segment Of(Node node)
		{
			for (; node != null; node = node.Parent) {
				if (node is Segment segment) {
					return segment;
				}
			}

			return null;
		}

		protected override void RunBuilder()
		{
			m_build(this);
		}
	}
}
