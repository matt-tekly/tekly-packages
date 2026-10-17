using System;
using Tekly.DevBoard.Components.Inputs;
using UnityEngine;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// Settings shared by the text and number inputs.
	/// </summary>
	public abstract class InputNode<T> : Node
	{
		public Binding<T> Binding { get; }
		public string Placeholder { get; private set; }
		public InputMode InputMode { get; private set; } = InputMode.Delayed;

		/// <summary>
		/// The most characters wide the field grows, or 0 to fill the row.
		/// </summary>
		public int MaxInputWidth { get; private set; }

		protected InputNode(string label, Binding<T> binding) : base(label)
		{
			Binding = binding ?? throw new ArgumentNullException(nameof(binding));
		}

		protected void SetMaxInputWidth(int characters)
		{
			EnsureEditable();
			MaxInputWidth = characters;
		}

		protected void SetPlaceholder(string placeholder)
		{
			EnsureEditable();
			Placeholder = placeholder;
		}

		protected void SetInputMode(InputMode inputMode)
		{
			EnsureEditable();
			InputMode = inputMode;
		}
	}

	public sealed class TextInputNode : InputNode<string>
	{
		public TextInputNode(string label, Binding<string> binding) : base(label, binding)
		{
		}

		/// <summary>
		/// The text shown while the field is empty.
		/// </summary>
		public TextInputNode WithPlaceholder(string placeholder)
		{
			SetPlaceholder(placeholder);
			return this;
		}

		/// <summary>
		/// When typed text reaches the setter. Delayed by default.
		/// </summary>
		public TextInputNode WithInputMode(InputMode inputMode)
		{
			SetInputMode(inputMode);
			return this;
		}

		/// <summary>
		/// Stops the field growing past this many characters wide, so a short value doesn't sit in a field
		/// stretched across a wide panel. It still shrinks when the panel is narrow. 0 fills the row again.
		/// </summary>
		public TextInputNode WithMaxInputWidth(int characters)
		{
			SetMaxInputWidth(characters);
			return this;
		}
	}

	/// <summary>
	/// A number input over doubles. The IntInput and FloatInput factories convert, and set whole numbers for ints.
	/// </summary>
	public sealed class NumberInputNode : InputNode<double>
	{
		public bool WholeNumbers { get; private set; }
		public string Format { get; private set; }

		/// <summary>
		/// How much the value changes per pixel dragged on the label. Null for the default.
		/// </summary>
		public double? DragStep { get; private set; }

		public bool DragScrub { get; private set; } = true;

		public NumberInputNode(string label, Binding<double> binding) : base(label, binding)
		{
		}

		public NumberInputNode WithPlaceholder(string placeholder)
		{
			SetPlaceholder(placeholder);
			return this;
		}

		/// <summary>
		/// When typed numbers reach the setter. Delayed by default.
		/// </summary>
		public NumberInputNode WithInputMode(InputMode inputMode)
		{
			SetInputMode(inputMode);
			return this;
		}

		/// <summary>
		/// Stops the field growing past this many characters wide, e.g. so counts in a list don't sit in fields
		/// stretched across a wide panel. It still shrinks when the panel is narrow. 0 fills the row again.
		/// </summary>
		public NumberInputNode WithMaxInputWidth(int characters)
		{
			SetMaxInputWidth(characters);
			return this;
		}

		public NumberInputNode WithWholeNumbers(bool wholeNumbers)
		{
			EnsureEditable();
			WholeNumbers = wholeNumbers;
			return this;
		}

		/// <summary>
		/// The numeric format string used to display the value, e.g. "0.00".
		/// </summary>
		public NumberInputNode WithFormat(string format)
		{
			EnsureEditable();
			Format = format;
			return this;
		}

		public NumberInputNode WithDragStep(double? step)
		{
			EnsureEditable();
			DragStep = step;
			return this;
		}

		/// <summary>
		/// Turns changing the value by dragging on the label on or off. On by default.
		/// </summary>
		public NumberInputNode WithDragScrub(bool enabled)
		{
			EnsureEditable();
			DragScrub = enabled;
			return this;
		}
	}

	/// <summary>
	/// A Vector3 edited as an X, Y and Z number input. Each input writes the whole vector back with its own part
	/// changed.
	/// </summary>
	public sealed class Vector3InputNode : Node
	{
		public Binding<Vector3> Binding { get; }

		/// <summary>
		/// The numeric format string each part is shown with, e.g. "0.00". Null for the default.
		/// </summary>
		public string Format { get; private set; }

		public Vector3InputNode(string label, Binding<Vector3> binding) : base(label)
		{
			Binding = binding ?? throw new ArgumentNullException(nameof(binding));
		}

		public Vector3InputNode WithFormat(string format)
		{
			EnsureEditable();
			Format = format;
			return this;
		}
	}

	public static class InputNodeFactory
	{
		public static TextInputNode TextInput(this ContainerNode parent, string label, Func<string> getValue, Action<string> setValue,
			Style<TextInputNode> style = default)
		{
			return parent.TextInput(label, new Binding<string>(getValue, setValue), style);
		}

		public static TextInputNode TextInput(this ContainerNode parent, string label, Binding<string> binding,
			Style<TextInputNode> style = default)
		{
			var node = parent.Add(new TextInputNode(label, binding));
			node.SetStyle(style.Name);

			return node;
		}

		public static NumberInputNode NumberInput(this ContainerNode parent, string label, Func<double> getValue,
			Action<double> setValue, Style<NumberInputNode> style = default)
		{
			return parent.NumberInput(label, new Binding<double>(getValue, setValue), style);
		}

		public static NumberInputNode NumberInput(this ContainerNode parent, string label, Binding<double> binding,
			Style<NumberInputNode> style = default)
		{
			var node = parent.Add(new NumberInputNode(label, binding));
			node.SetStyle(style.Name);

			return node;
		}

		public static NumberInputNode IntInput(this ContainerNode parent, string label, Func<int> getValue, Action<int> setValue,
			Style<NumberInputNode> style = default)
		{
			var binding = new Binding<int>(getValue, setValue).Convert(value => (double) value, ToInt);
			return parent.NumberInput(label, binding, style).WithWholeNumbers(true);
		}

		public static NumberInputNode FloatInput(this ContainerNode parent, string label, Func<float> getValue, Action<float> setValue,
			Style<NumberInputNode> style = default)
		{
			var binding = new Binding<float>(getValue, setValue).Convert(value => (double) value, value => (float) value);
			return parent.NumberInput(label, binding, style);
		}

		/// <summary>
		/// X, Y and Z inputs after the label, e.g. for a position. In a form the label lines up with the others.
		/// </summary>
		public static Vector3InputNode Vector3Input(this ContainerNode parent, string label, Func<Vector3> getValue,
			Action<Vector3> setValue)
		{
			return parent.Add(new Vector3InputNode(label, new Binding<Vector3>(getValue, setValue)));
		}

		/// <summary>
		/// A typed number as an int, clamped to the int range.
		/// </summary>
		private static int ToInt(double value)
		{
			return (int) Math.Round(Math.Clamp(value, int.MinValue, int.MaxValue));
		}
	}
}
