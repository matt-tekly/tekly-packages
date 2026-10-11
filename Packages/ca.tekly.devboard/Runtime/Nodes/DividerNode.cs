namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// A line across the parent's layout axis: horizontal in a vertical container, vertical in a row.
	/// </summary>
	public sealed class DividerNode : Node
	{
		public DividerNode() : base(null)
		{
		}
	}

	public static class DividerNodeFactory
	{
		public static DividerNode Divider(this ContainerNode parent, Style<DividerNode> style = default)
		{
			var node = parent.Add(new DividerNode());
			node.SetStyle(style.Name);

			return node;
		}
	}
}
