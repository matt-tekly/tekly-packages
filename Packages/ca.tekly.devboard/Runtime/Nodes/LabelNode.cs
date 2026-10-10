using Tekly.DevBoard.Components;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// Plain text, or a heading for titling a group of nodes. Text that changes belongs in a property.
	/// </summary>
	public sealed class LabelNode : Node
	{
		public string Text => Label;
		public bool IsHeading { get; }

		public LabelNode(string text, bool isHeading) : base(text)
		{
			IsHeading = isHeading;
		}
	}

	public static class LabelNodeFactory
	{
		public static LabelNode Label(this ContainerNode parent, string text, WidgetPrefab<LabelWidget> prefab = default)
		{
			var node = parent.Add(new LabelNode(text, false));
			node.SetPrefab(prefab.Name);

			return node;
		}

		/// <summary>
		/// A label styled as a heading, for titling a group of nodes.
		/// </summary>
		public static LabelNode Heading(this ContainerNode parent, string text, WidgetPrefab<LabelWidget> prefab = default)
		{
			var node = parent.Add(new LabelNode(text, true));
			node.SetPrefab(prefab.Name);

			return node;
		}
	}
}
