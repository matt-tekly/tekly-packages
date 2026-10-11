using System;
using UnityEngine;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// A slider over a float, with an optional number input beside it. With whole numbers on it snaps to integers,
	/// which is how int sliders are made.
	/// </summary>
	public sealed class SliderNode : Node
	{
		public Binding<float> Binding { get; }
		public float Min { get; }
		public float Max { get; }
		public bool WholeNumbers { get; }

		/// <summary>
		/// The numeric format string the value is shown with, e.g. "0.00". Null for the default.
		/// </summary>
		public string Format { get; private set; }

		public bool ShowValueInput { get; private set; } = true;
		public bool ShowValueInLabel { get; private set; }

		public SliderNode(string label, float min, float max, bool wholeNumbers, Binding<float> binding) : base(label)
		{
			Binding = binding ?? throw new ArgumentNullException(nameof(binding));
			Min = min;
			Max = max;
			WholeNumbers = wholeNumbers;
		}

		public SliderNode WithFormat(string format)
		{
			EnsureEditable();
			Format = format;
			return this;
		}

		/// <summary>
		/// Shows or hides the value input beside the slider. Hidden, the slider takes the room.
		/// </summary>
		public SliderNode WithValueInput(bool show)
		{
			EnsureEditable();
			ShowValueInput = show;
			return this;
		}

		/// <summary>
		/// Shows the value on a second line under the label. Pair it with WithValueInput(false) for a compact
		/// slider that still shows its value.
		/// </summary>
		public SliderNode WithValueInLabel(bool show)
		{
			EnsureEditable();
			ShowValueInLabel = show;
			return this;
		}
	}

	public static class SliderNodeFactory
	{
		public static SliderNode Slider(this ContainerNode parent, string label, float min, float max, Func<float> getValue,
			Action<float> setValue, Style<SliderNode> style = default)
		{
			return parent.Slider(label, min, max, new Binding<float>(getValue, setValue), style);
		}

		public static SliderNode Slider(this ContainerNode parent, string label, float min, float max, Binding<float> binding,
			Style<SliderNode> style = default)
		{
			var node = parent.Add(new SliderNode(label, min, max, false, binding));
			node.SetStyle(style.Name);

			return node;
		}

		/// <summary>
		/// A slider that snaps to whole numbers.
		/// </summary>
		public static SliderNode SliderInt(this ContainerNode parent, string label, int min, int max, Func<int> getValue,
			Action<int> setValue, Style<SliderNode> style = default)
		{
			var binding = new Binding<int>(getValue, setValue).Convert(value => (float) value, Mathf.RoundToInt);
			var node = parent.Add(new SliderNode(label, min, max, true, binding));
			node.SetStyle(style.Name);

			return node;
		}
	}
}
