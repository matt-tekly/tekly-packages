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

		public override string Path => string.IsNullOrEmpty(Source.Path) ? Id : $"{Source.Path}/{Id}";

		private readonly Action<Segment> m_build;

		internal Segment(PageSegment source, Action<Segment> build) : base(source.Title, null)
		{
			Source = source;
			m_build = build;
			Id = source.Title ?? "Segment";
		}

		protected override void RunBuilder()
		{
			m_build(this);
		}
	}
}
