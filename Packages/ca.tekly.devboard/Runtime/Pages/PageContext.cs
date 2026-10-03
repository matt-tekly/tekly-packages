using Tekly.DevBoard.Components;
using Tekly.DevBoard.Panels;

namespace Tekly.DevBoard.Pages
{
	/// <summary>
	/// Passed to a segment's builder. A builder can run many times: once for every panel showing the page, and
	/// again whenever the segment is rebuilt. So builders should read values through getters, not keep widget
	/// references outside the builder, and undo anything they hook up (events, subscriptions) with OnDispose.
	///
	/// OnDispose runs when this copy of the segment is torn down: the segment is disposed or rebuilt, its page
	/// is removed, or its panel is closed.
	/// </summary>
	public class PageContext : BuildContext
	{
		/// <summary>
		/// The panel showing this copy of the page.
		/// </summary>
		public DevBoardPanel Panel { get; }

		public PageSegment Segment { get; }
		public string Path => Segment.Path;

		/// <summary>
		/// True when the panel is a passive overlay. Builders can use it to show a more compact version.
		/// </summary>
		public bool IsOverlay => Panel.IsOverlay;

		internal PageContext(DevBoardPanel panel, PageSegment segment, ContainerWidget root) : base(root)
		{
			Panel = panel;
			Segment = segment;
		}

		/// <summary>
		/// Navigates this panel to another page.
		/// </summary>
		public void Open(string path)
		{
			Panel.Open(path);
		}

		/// <summary>
		/// Tears this segment down and builds it again, in this panel, at the end of the frame.
		/// </summary>
		public void Rebuild()
		{
			Panel.RequestRebuild(Segment);
		}
	}
}
