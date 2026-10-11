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
		public static PropertyNode Property<T>(this ContainerNode parent, string label, Func<T> getValue, string format = "{0}",
			Style<PropertyNode> style = default)
		{
			return parent.Property(label, new Binding<T>(getValue), format, style);
		}

		/// <summary>
		/// Shows a float, skipping the redraw while it stays within epsilon of the last value shown.
		/// </summary>
		public static PropertyNode Property(this ContainerNode parent, string label, Func<float> getValue, float epsilon,
			string format = "{0}", Style<PropertyNode> style = default)
		{
			return parent.Property(label, new Binding<float>(getValue, isSame: BindingCompare.Within(epsilon)), format, style);
		}

		public static PropertyNode Property<T>(this ContainerNode parent, string label, Binding<T> binding, string format = "{0}",
			Style<PropertyNode> style = default)
		{
			var node = parent.Add(new PropertyNode<T>(label, binding, format));
			node.SetStyle(style.Name);

			return node;
		}
	}
}
