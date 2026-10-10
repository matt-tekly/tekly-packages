using System.Collections.Generic;
using Tekly.DevBoard.Nodes;

namespace Tekly.DevBoard.Views
{
	/// <summary>
	/// What a host shows for one node: a widget in a panel, or what a remote viewer was sent. Mirrors decide what's
	/// shown and in what order; elements only carry it out.
	/// </summary>
	public interface IViewElement
	{
		void SetShown(bool shown);

		/// <summary>
		/// Removes the element and everything inside it.
		/// </summary>
		void Destroy();
	}

	/// <summary>
	/// The element for a container node: it makes the elements for the node's children.
	/// </summary>
	public interface IContainerElement : IViewElement
	{
		/// <summary>
		/// False while the children can't be seen, e.g. a closed foldout, so they aren't refreshed.
		/// </summary>
		bool ChildrenVisible { get; }

		/// <summary>
		/// Makes the children visible, e.g. opens a closed foldout, to reveal one of them.
		/// </summary>
		void ShowChildren();

		IViewElement CreateChild(Node child);

		/// <summary>
		/// Puts the children's elements in this order. Children that aren't shown are left out.
		/// </summary>
		void Order(IReadOnlyList<IViewElement> children);
	}

	/// <summary>
	/// The element for a list node. A searchable list's element also shows the search field and a summary of the
	/// results.
	/// </summary>
	public interface IListElement : IContainerElement
	{
		/// <summary>
		/// Called once, before any rows are created, for searchable lists. query reads and writes the search text.
		/// </summary>
		void ShowSearch(Binding<string> query, string placeholder);

		void ShowSummary(ListSummary summary);

		/// <summary>
		/// A new search was typed: go back to the first result.
		/// </summary>
		void ScrollToStart();
	}

	/// <summary>
	/// How a search went, for the line under the results.
	/// </summary>
	public readonly struct ListSummary
	{
		public readonly int Shown;

		/// <summary>
		/// Rows that matched but weren't shown, past the list's MaxResults.
		/// </summary>
		public readonly int Hidden;

		public readonly bool HasQuery;

		/// <summary>
		/// Whether the list shows rows while nothing is typed.
		/// </summary>
		public readonly bool ShowAllWhenEmpty;

		public ListSummary(int shown, int hidden, bool hasQuery, bool showAllWhenEmpty)
		{
			Shown = shown;
			Hidden = hidden;
			HasQuery = hasQuery;
			ShowAllWhenEmpty = showAllWhenEmpty;
		}
	}
}
