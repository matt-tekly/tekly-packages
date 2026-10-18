namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// A strip of tabs with one tab's content showing at a time. Add tabs with Tab; anything else added to it isn't
	/// shown. Each panel remembers which tab is selected, keyed by the tab set's path, and only builds a tab's
	/// widgets the first time it's selected.
	/// </summary>
	public sealed class TabsNode : ContainerNode
	{
		/// <summary>
		/// The label of the tab selected in a panel with no saved state for it. Null for the first tab.
		/// </summary>
		public string StartTab { get; private set; }

		public TabsNode() : base(ContainerKind.Column)
		{
		}

		internal override string DefaultId => "tabs";

		/// <summary>
		/// Always a level: each panel saves the selected tab by its path.
		/// </summary>
		internal override bool AddsPathLevel => true;

		public TabsNode WithStartTab(string label)
		{
			EnsureEditable();
			StartTab = label;
			return this;
		}
	}

	/// <summary>
	/// One tab of a TabsNode: its label goes on the tab's button and its children show while it's selected.
	/// </summary>
	public sealed class TabNode : ContainerNode
	{
		public TabNode(string label) : base(ContainerKind.Column, label)
		{
		}

		internal override string DefaultId => "tab";

		internal override bool AddsPathLevel => true;
	}

	public static class TabsNodeFactory
	{
		public static TabsNode Tabs(this ContainerNode parent)
		{
			return parent.Add(new TabsNode());
		}

		/// <summary>
		/// Adds a tab. Its children are laid out like a Column's.
		/// </summary>
		public static TabNode Tab(this TabsNode tabs, string label)
		{
			return tabs.Add(new TabNode(label));
		}
	}
}
