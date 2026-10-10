using System;
using Tekly.DevBoard.Components;

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
			WidgetPrefab<ToggleWidget> prefab = default)
		{
			return parent.Toggle(label, new Binding<bool>(getValue, setValue), prefab);
		}

		public static ToggleNode Toggle(this ContainerNode parent, string label, Binding<bool> binding,
			WidgetPrefab<ToggleWidget> prefab = default)
		{
			var node = parent.Add(new ToggleNode(label, binding));
			node.SetPrefab(prefab.Name);

			return node;
		}
	}
}
