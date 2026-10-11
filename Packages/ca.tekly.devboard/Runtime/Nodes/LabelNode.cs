namespace Tekly.DevBoard.Nodes
{
	public enum LabelKind
	{
		Normal,

		/// For titling a group of nodes
		Heading,

		/// Something went wrong, e.g. a builder threw
		Error
	}

	/// <summary>
	/// Plain text, or a heading for titling a group of nodes. Text that changes belongs in a property.
	/// </summary>
	public sealed class LabelNode : Node
	{
		public string Text => Label;
		public LabelKind Kind { get; }

		public LabelNode(string text, LabelKind kind = LabelKind.Normal) : base(text)
		{
			Kind = kind;
		}
	}

	public static class LabelNodeFactory
	{
		public static LabelNode Label(this ContainerNode parent, string text, Style<LabelNode> style = default)
		{
			return parent.Label(text, LabelKind.Normal, style);
		}

		/// <summary>
		/// A label styled as a heading, for titling a group of nodes.
		/// </summary>
		public static LabelNode Heading(this ContainerNode parent, string text, Style<LabelNode> style = default)
		{
			return parent.Label(text, LabelKind.Heading, style);
		}

		public static LabelNode Label(this ContainerNode parent, string text, LabelKind kind, Style<LabelNode> style = default)
		{
			var node = parent.Add(new LabelNode(text, kind));
			node.SetStyle(style.Name);

			return node;
		}
	}
}
