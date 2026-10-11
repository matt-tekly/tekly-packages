using System;

namespace Tekly.DevBoard.Nodes
{
	public sealed class ToggleNode : Node
	{
		public Binding<bool> Binding { get; }

		public ToggleNode(string label, Binding<bool> binding) : base(label)
		{
			Binding = binding ?? throw new ArgumentNullException(nameof(binding));
		}
	}

	public static class ToggleNodeFactory
	{
		public static ToggleNode Toggle(this ContainerNode parent, string label, Func<bool> getValue, Action<bool> setValue,
			Style<ToggleNode> style = default)
		{
			return parent.Toggle(label, new Binding<bool>(getValue, setValue), style);
		}

		public static ToggleNode Toggle(this ContainerNode parent, string label, Binding<bool> binding,
			Style<ToggleNode> style = default)
		{
			var node = parent.Add(new ToggleNode(label, binding));
			node.SetStyle(style.Name);

			return node;
		}
	}
}
