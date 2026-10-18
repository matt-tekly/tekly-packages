using System;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// Lets code that doesn't know a binding's type use it with its type, e.g. a renderer handing a property's
	/// binding to a widget.
	/// </summary>
	public interface IBindingVisitor
	{
		void Visit<T>(Binding<T> binding);
	}

	/// <summary>
	/// A labelled read-only value, formatted and redrawn as it changes.
	/// </summary>
	public abstract class PropertyNode : Node
	{
		public string Format { get; }
		public bool Monospaced { get; private set; }

		/// <summary>
		/// How many characters wide the value is, or 0 to fit the value.
		/// </summary>
		public int ValueWidth { get; private set; }

		protected PropertyNode(string label, string format) : base(label)
		{
			Format = format ?? "{0}";
		}

		/// <summary>
		/// Shows the value in the monospace text style, so digits don't jitter as they change.
		/// </summary>
		public PropertyNode WithMonospaced(bool monospaced = true)
		{
			EnsureEditable();
			Monospaced = monospaced;
			return this;
		}

		/// <summary>
		/// Makes the value always as wide as this many characters, cutting longer values off with an ellipsis. Keeps
		/// a layout still while values change length, e.g. columns of numbers side by side.
		/// </summary>
		public PropertyNode WithValueWidth(int characters)
		{
			EnsureEditable();
			ValueWidth = characters;
			return this;
		}

		/// <summary>
		/// Calls visitor with the binding, typed.
		/// </summary>
		public abstract void Accept(IBindingVisitor visitor);
	}

	public sealed class PropertyNode<T> : PropertyNode
	{
		public Binding<T> Binding { get; }

		public PropertyNode(string label, Binding<T> binding, string format) : base(label, format)
		{
			Binding = binding ?? throw new ArgumentNullException(nameof(binding));
		}

		public override void Accept(IBindingVisitor visitor)
		{
			visitor.Visit(Binding);
		}
	}

	public static class PropertyNodeFactory
	{
		/// <summary>
		/// Shows the value of getValue with format, updating as it changes.
		/// </summary>
		public static PropertyNode Property<T>(this ContainerNode parent, string label, Func<T> getValue, string format = "{0}")
		{
			return parent.Property(label, new Binding<T>(getValue), format);
		}

		/// <summary>
		/// Shows a float, skipping the redraw while it stays within epsilon of the last value shown.
		/// </summary>
		public static PropertyNode Property(this ContainerNode parent, string label, Func<float> getValue, float epsilon,
			string format = "{0}")
		{
			return parent.Property(label, new Binding<float>(getValue, isSame: BindingCompare.Within(epsilon)), format);
		}

		public static PropertyNode Property<T>(this ContainerNode parent, string label, Binding<T> binding, string format = "{0}")
		{
			return parent.Add(new PropertyNode<T>(label, binding, format));
		}
	}
}
