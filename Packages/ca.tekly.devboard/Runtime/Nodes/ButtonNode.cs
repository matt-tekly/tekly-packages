using System;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Pages;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// A button that runs an action. Rebuilds started from the action happen at the end of the frame, so it can
	/// rebuild the scope it's in.
	/// </summary>
	public sealed class ButtonNode : Node
	{
		public Action Action { get; }

		public ButtonNode(string label, Action action) : base(label)
		{
			Action = action;
		}
	}

	/// <summary>
	/// A button that takes the panel it's clicked in to another page.
	/// </summary>
	public sealed class LinkNode : Node
	{
		public string Target { get; }

		public LinkNode(string label, string target) : base(label)
		{
			Target = PageTree.NormalizePath(target);
		}
	}

	public static class ButtonNodeFactory
	{
		public static ButtonNode Button(this ContainerNode parent, string label, Action action, WidgetPrefab<ButtonWidget> prefab = default)
		{
			var node = parent.Add(new ButtonNode(label, action));
			node.SetPrefab(prefab.Name);

			return node;
		}

		/// <summary>
		/// A button that opens the page at path in the panel it's clicked in.
		/// </summary>
		public static LinkNode Link(this ContainerNode parent, string label, string path, WidgetPrefab<ButtonWidget> prefab = default)
		{
			var node = parent.Add(new LinkNode(label, path));
			node.SetPrefab(prefab.Name);

			return node;
		}
	}
}
