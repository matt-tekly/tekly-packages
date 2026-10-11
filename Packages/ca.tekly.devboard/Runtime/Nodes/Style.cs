namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// A named look for a kind of node, instead of its usual one, e.g. a button shown as a page button. Typed with
	/// the node it's for, so a slider's style can't be given to a button. Panels show a node with the registered
	/// widget prefab of that name; other front ends can map it however suits them, or ignore it.
	///
	/// Built-in ones are in <see cref="Styles"/>. A game with its own prefabs can declare its own the same way:
	/// <code>public static readonly Style&lt;ButtonNode&gt; BigButton = "button_big";</code>
	/// The default (no name) means the node's usual look.
	/// </summary>
	public readonly struct Style<TNode> where TNode : Node
	{
		public string Name { get; }

		public bool IsSet => Name != null;

		public Style(string name)
		{
			Name = name;
		}

		public static implicit operator Style<TNode>(string name)
		{
			return new Style<TNode>(name);
		}

		public override string ToString()
		{
			return Name ?? "(default)";
		}
	}

	/// <summary>
	/// The built-in styles: the looks DevBoard's own widget prefabs offer besides each node's usual one.
	/// </summary>
	public static class Styles
	{
		public static readonly Style<ButtonNode> PageButton = "button_page";
		public static readonly Style<ButtonNode> IconButton = "button_icon";
		public static readonly Style<TextInputNode> VerticalTextInput = "textinput_vertical";
		public static readonly Style<TextInputNode> SearchInput = "textinput_search";
	}
}
