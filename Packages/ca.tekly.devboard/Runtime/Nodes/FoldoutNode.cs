namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// A container that opens and closes. Each panel remembers whether it's open, keyed by the foldout's path.
	/// </summary>
	public sealed class FoldoutNode : ContainerNode
	{
		/// <summary>
		/// Whether the foldout starts open in a panel with no saved state for it.
		/// </summary>
		public bool StartExpanded { get; private set; }

		public FoldoutNode(string label) : base(ContainerKind.Column, label)
		{
		}

		internal override string DefaultId => "foldout";

		public FoldoutNode WithStartExpanded(bool expanded = true)
		{
			EnsureEditable();
			StartExpanded = expanded;
			return this;
		}
	}

	public static class FoldoutNodeFactory
	{
		public static FoldoutNode Foldout(this ContainerNode parent, string label, Style<FoldoutNode> style = default)
		{
			var node = parent.Add(new FoldoutNode(label));
			node.SetStyle(style.Name);

			return node;
		}
	}
}
