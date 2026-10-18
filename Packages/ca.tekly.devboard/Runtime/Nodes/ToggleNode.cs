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
		public static ToggleNode Toggle(this ContainerNode parent, string label, Func<bool> getValue, Action<bool> setValue)
		{
			return parent.Toggle(label, new Binding<bool>(getValue, setValue));
		}

		public static ToggleNode Toggle(this ContainerNode parent, string label, Binding<bool> binding)
		{
			return parent.Add(new ToggleNode(label, binding));
		}
	}
}
