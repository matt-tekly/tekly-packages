using Tekly.DevBoard.Nodes;
using Tekly.DevBoard.Panels;

namespace Tekly.DevBoard.Views
{
	/// <summary>
	/// What a renderer knows about the panel it's rendering a node into.
	/// </summary>
	public sealed class ViewContext
	{
		public DevBoardPanel Panel { get; }

		/// <summary>
		/// True when the panel is a passive overlay.
		/// </summary>
		public bool IsOverlay => Panel.IsOverlay;

		internal ViewContext(DevBoardPanel panel)
		{
			Panel = panel;
		}

		/// <summary>
		/// The DevBoard.State key for a piece of a node's view state in this panel, e.g. whether a foldout is open.
		/// Panels keep their own view state, so the same node can be open in one and closed in another.
		/// </summary>
		public string StateKey(Node node, string local)
		{
			return $"{Panel.Id}/{node.Path}/{local}";
		}

		/// <summary>
		/// Takes the panel to another page, after the current click or tick has finished.
		/// </summary>
		public void Open(string path)
		{
			Panel.OpenDeferred(path);
		}
	}
}
