using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// Picks one of a list of choices. Works on indexes, so the Dropdown factories can bind it to strings, enums or
	/// any other list.
	/// </summary>
	public sealed class DropdownNode : Node
	{
		/// <summary>
		/// How many choices there are. Read each time the dropdown refreshes or opens, so the choices can change.
		/// </summary>
		public Func<int> GetCount { get; }

		public Func<int, DropdownChoice> GetChoice { get; }

		/// <summary>
		/// The index of the current choice, -1 for none.
		/// </summary>
		public Binding<int> Index { get; }

		public bool Searchable { get; private set; }

		public DropdownNode(string label, Func<int> getCount, Func<int, DropdownChoice> getChoice, Binding<int> index) : base(label)
		{
			GetCount = getCount ?? throw new ArgumentNullException(nameof(getCount));
			GetChoice = getChoice ?? throw new ArgumentNullException(nameof(getChoice));
			Index = index ?? throw new ArgumentNullException(nameof(index));
		}

		/// <summary>
		/// Shows a search field over the choices when opened, for long lists.
		/// </summary>
		public DropdownNode WithSearch(bool searchable = true)
		{
			EnsureEditable();
			Searchable = searchable;
			return this;
		}
	}

	public static class DropdownNodeFactory
	{
		/// <summary>
		/// A dropdown over a list of items. choices is read each time the dropdown refreshes or opens, so it can change.
		/// getChoice is what an option shows for an item, e.g. a title and subtitle. A getter value that isn't in
		/// choices shows as no choice.
		/// </summary>
		public static DropdownNode Dropdown<T>(this ContainerNode parent, string label, IReadOnlyList<T> choices,
			Func<T, DropdownChoice> getChoice, Func<T> getValue, Action<T> setValue, Style<DropdownNode> style = default)
		{
			var comparer = EqualityComparer<T>.Default;

			int IndexOf(T value)
			{
				for (var i = 0; i < choices.Count; i++) {
					if (comparer.Equals(choices[i], value)) {
						return i;
					}
				}

				return -1;
			}

			var index = new Binding<int>(() => IndexOf(getValue()), i => setValue(choices[i]));
			var node = parent.Add(new DropdownNode(label, () => choices.Count, i => getChoice(choices[i]), index));
			node.SetStyle(style.Name);

			return node;
		}

		/// <summary>
		/// A dropdown over a list of items, each shown as a line of text from getText.
		/// </summary>
		public static DropdownNode Dropdown<T>(this ContainerNode parent, string label, IReadOnlyList<T> choices,
			Func<T, string> getText, Func<T> getValue, Action<T> setValue, Style<DropdownNode> style = default)
		{
			return parent.Dropdown(label, choices, item => new DropdownChoice(getText(item)), getValue, setValue, style);
		}

		public static DropdownNode Dropdown(this ContainerNode parent, string label, IReadOnlyList<string> choices,
			Func<string> getValue, Action<string> setValue, Style<DropdownNode> style = default)
		{
			return parent.Dropdown(label, choices, s => s, getValue, setValue, style);
		}

		/// <summary>
		/// A dropdown over every value of an enum.
		/// </summary>
		public static DropdownNode Dropdown<TEnum>(this ContainerNode parent, string label, Func<TEnum> getValue,
			Action<TEnum> setValue, Style<DropdownNode> style = default) where TEnum : struct, Enum
		{
			var values = (TEnum[]) Enum.GetValues(typeof(TEnum));
			return parent.Dropdown(label, values, v => v.ToString(), getValue, setValue, style);
		}
	}
}
