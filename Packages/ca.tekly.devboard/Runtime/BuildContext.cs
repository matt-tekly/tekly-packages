using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components;
using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Handed to code that builds a piece of DevBoard UI which DevBoard may tear down and build again, like a
	/// page segment or a search result row. Build into Root, and undo anything you hook up (events,
	/// subscriptions, spawned objects) with OnDispose.
	/// </summary>
	public abstract class BuildContext
	{
		/// <summary>
		/// The container to build into.
		/// </summary>
		public ContainerWidget Root { get; }

		/// <summary>
		/// True once this copy has been torn down.
		/// </summary>
		public bool IsDisposed { get; private set; }

		private List<Action> m_onDispose;

		protected BuildContext(ContainerWidget root)
		{
			Root = root;
		}

		/// <summary>
		/// Runs action when this copy is torn down. Callbacks run in the reverse order they were added.
		/// </summary>
		public void OnDispose(Action action)
		{
			if (action == null) {
				return;
			}

			// Already torn down, e.g. a callback registered from a delayed call: run it straight away
			if (IsDisposed) {
				Run(action);
				return;
			}

			m_onDispose ??= new List<Action>();
			m_onDispose.Add(action);
		}

		/// <summary>
		/// A search field with a list of results under it, added to Root. getItems is read each time a search
		/// runs, getText is the text a search matches against, and buildRow draws one result. The rows are
		/// disposed along with this context.
		/// </summary>
		public SearchList<T> Search<T>(Func<IEnumerable<T>> getItems, Func<T, string> getText, Action<SearchRow<T>> buildRow,
			string placeholder = "Search")
		{
			return Search(Root, getItems, getText, buildRow, placeholder);
		}

		/// <summary>
		/// A search added to parent, a container somewhere inside Root, e.g. a card or form.
		/// </summary>
		public SearchList<T> Search<T>(ContainerWidget parent, Func<IEnumerable<T>> getItems, Func<T, string> getText,
			Action<SearchRow<T>> buildRow, string placeholder = "Search")
		{
			var root = ContainerWidget.CreatePlain(parent.Content, "Search");
			var search = new SearchList<T>(root, getItems, getText, buildRow, placeholder);
			OnDispose(search.Dispose);

			return search;
		}

		internal void Dispose()
		{
			if (IsDisposed) {
				return;
			}

			IsDisposed = true;

			if (m_onDispose == null) {
				return;
			}

			for (var i = m_onDispose.Count - 1; i >= 0; i--) {
				Run(m_onDispose[i]);
			}

			m_onDispose = null;
		}

		private static void Run(Action action)
		{
			try {
				action();
			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}
	}
}
