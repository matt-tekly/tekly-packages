using Tekly.DevBoard.Components;

namespace Tekly.DevBoard.Nodes
{
	public enum LabelStyle
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
		public LabelStyle Style { get; }

		public LabelNode(string text, LabelStyle style = LabelStyle.Normal) : base(text)
		{
			Style = style;
		}
	}

	public static class LabelNodeFactory
	{
		public static LabelNode Label(this ContainerNode parent, string text, WidgetPrefab<LabelWidget> prefab = default)
		{
			return parent.Label(text, LabelStyle.Normal, prefab);
		}

		/// <summary>
		/// A label styled as a heading, for titling a group of nodes.
		/// </summary>
		public static LabelNode Heading(this ContainerNode parent, string text, WidgetPrefab<LabelWidget> prefab = default)
		{
			return parent.Label(text, LabelStyle.Heading, prefab);
		}

		public static LabelNode Label(this ContainerNode parent, string text, LabelStyle style, WidgetPrefab<LabelWidget> prefab = default)
		{
			var node = parent.Add(new LabelNode(text, style));
			node.SetPrefab(prefab.Name);

			return node;
		}
	}
}
