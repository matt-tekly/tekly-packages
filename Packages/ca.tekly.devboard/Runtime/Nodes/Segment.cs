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
			Id = UniqueId(source);
		}

		/// <summary>
		/// The segment's slug, made unique among its page's segments. Ones with the same slug are numbered in the
		/// order they're on the page, so a path means the same segment from one run to the next, and then past any
		/// id a sibling built earlier already took.
		/// </summary>
		private static string UniqueId(PageSegment source)
		{
			var baseId = BaseId(source);
			var page = source.Node;

			if (page == null) {
				return baseId;
			}

			var number = 1;

			foreach (var sibling in page.Segments) {
				if (sibling == source) {
					break;
				}

				if (BaseId(sibling) == baseId) {
					number++;
				}
			}

			var id = Numbered(baseId, number);

			while (IsTaken(page, source, id)) {
				id = Numbered(baseId, ++number);
			}

			return id;
		}

		private static string BaseId(PageSegment segment)
		{
			return NodePaths.Slug(segment.Title) ?? "segment";
		}

		private static string Numbered(string baseId, int number)
		{
			return number == 1 ? baseId : $"{baseId}-{number}";
		}

		private static bool IsTaken(PageNode page, PageSegment source, string id)
		{
			foreach (var sibling in page.Segments) {
				if (sibling != source && sibling.BuiltRoot?.Id == id) {
					return true;
				}
			}

			return false;
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
