using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components.Inputs;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// A search field with a list of results under it. Results come from a list of items, filtered by the text
	/// typed, and each one is drawn by a row builder you supply, so a result can be any widgets you like.
	///
	/// The search runs when typing pauses. Only the first MaxResults matches are shown, with a count of the
	/// rest. The search text is kept in DevBoard.State, so it survives the search being rebuilt.
	///
	/// Create one with BuildContext.Search (e.g. page.Search), which also disposes the rows when that context is
	/// torn down.
	/// </summary>
	public class SearchList<T>
	{
		private const string DEFAULT_STATE_KEY = "search";
		private const string ERROR_COLOR = "#E5534B";
		private const int DEFAULT_RESULT_SPACING = 4;

		/// <summary>
		/// The container holding the search field and the results.
		/// </summary>
		public ContainerWidget Root { get; }

		public TextInputWidget Input { get; }

		/// <summary>
		/// The container rows are built in. With WithScroll, this is the scroll view's content.
		/// </summary>
		public ContainerWidget Results { get; private set; }
		public string Query => m_query;

		private readonly Func<IEnumerable<T>> m_getItems;
		private readonly Func<T, string> m_getText;
		private readonly Action<SearchRow<T>> m_buildRow;
		private readonly List<SearchRow<T>> m_rows = new();
		private readonly List<string> m_words = new();

		private ContainerWidget m_scrollFrame;
		private ScrollViewWidget m_scroll;
		private Func<T, string, bool> m_match;
		private string m_query = string.Empty;
		private string m_stateKey;
		private int m_maxResults = 30;
		private int m_resultSpacing = DEFAULT_RESULT_SPACING;
		private bool m_showAllWhenEmpty = true;
		private bool m_searching;
		private bool m_disposed;

		internal SearchList(ContainerWidget root, Func<IEnumerable<T>> getItems, Func<T, string> getText,
			Action<SearchRow<T>> buildRow, string placeholder)
		{
			Root = root;
			m_getItems = getItems;
			m_getText = getText;
			m_buildRow = buildRow;


			UseStateKey(DEFAULT_STATE_KEY);

			Input = root.TextInput(null, placeholder, () => m_query, SetQuery, InputMode.Debounced);
			Results = ContainerWidget.CreatePlain(root.Content, "Results", DEFAULT_RESULT_SPACING);

			RunSearch();
		}

		/// <summary>
		/// Most rows shown at once. The rest are counted at the end of the list.
		/// </summary>
		public SearchList<T> WithMaxResults(int maxResults)
		{
			m_maxResults = Mathf.Max(1, maxResults);
			Refresh();
			return this;
		}

		/// <summary>
		/// Puts the results in their own scroll view, at most maxHeight tall, so the search field stays put while
		/// the results scroll. Each new search scrolls back to the top.
		/// </summary>
		public SearchList<T> WithScroll(float maxHeight)
		{
			if (m_scroll == null) {
				DisposeRows();
				ContainerWidget.DestroyWidget(Results.gameObject);

				// The frame caps the height; the scroll view inside it shrinks to fit and scrolls the rest
				m_scrollFrame = ContainerWidget.CreatePlain(Root.Content, "Results");
				m_scroll = m_scrollFrame.ScrollView().WithoutSavedState();

				// Results are a vertical list whatever the scroll view prefab's content is set up as
				m_scroll.WithAxis(Common.Utils.LayoutAxis.Vertical).WithCrossAlignment(CrossAlignment.Stretch).WithSpacing(m_resultSpacing);
				Results = m_scroll;
			}

			m_scrollFrame.WithMaxHeight(maxHeight);

			// Rebuild now rather than next tick, so the page doesn't show empty results for a frame
			RunSearch();
			return this;
		}

		/// <summary>
		/// The gap between result rows. Defaults to 4, whether or not the results scroll.
		/// </summary>
		public SearchList<T> WithResultSpacing(int spacing)
		{
			m_resultSpacing = spacing;
			Results.WithSpacing(spacing);
			return this;
		}

		/// <summary>
		/// Whether an empty search shows the first items (the default) or nothing.
		/// </summary>
		public SearchList<T> ShowAllWhenEmpty(bool showAll)
		{
			m_showAllWhenEmpty = showAll;
			Refresh();
			return this;
		}

		/// <summary>
		/// Replaces the default matching (every typed word appears in the item's text, ignoring case).
		/// match gets the item and the trimmed search text, and is only called when the text isn't empty.
		/// </summary>
		public SearchList<T> WithMatch(Func<T, string, bool> match)
		{
			m_match = match;
			Refresh();
			return this;
		}

		/// <summary>
		/// Saves the search text under key instead of the default. Use it when one container holds two searches.
		/// </summary>
		public SearchList<T> WithStateKey(string key)
		{
			UseStateKey(key);
			Refresh();
			return this;
		}

		/// <summary>
		/// Runs the search again, e.g. after the items changed.
		/// </summary>
		public void Refresh()
		{
			RunSearch();
		}

		/// <summary>
		/// Tears the row down and builds it again in place.
		/// </summary>
		internal void RebuildRow(SearchRow<T> row)
		{
			var index = m_rows.IndexOf(row);

			// Replaced by a newer search, or asked to rebuild while it's still being built
			if (index < 0 || row.IsBuilding || m_disposed) {
				return;
			}

			row.Dispose();
			row.Root.Clear();
			BuildRow(row.Root, row.Item, index);
		}

		/// <summary>
		/// Disposes every row. Called when the context the search was created from is torn down.
		/// </summary>
		internal void Dispose()
		{
			if (m_disposed) {
				return;
			}

			m_disposed = true;
			DisposeRows();
		}

		private void UseStateKey(string key)
		{
			m_stateKey = DevBoardState.KeyFor(Root, key);

			var state = DevBoard.Instance?.State;

			if (state != null && state.TryGet(m_stateKey, out string saved)) {
				m_query = saved ?? string.Empty;
			}
		}

		private void SetQuery(string query)
		{
			query ??= string.Empty;

			if (query == m_query) {
				return;
			}

			m_query = query;
			DevBoard.Instance?.State.Set(m_stateKey, m_query);

			// The setter is called by the input's tick, outside any builder, so the results can be rebuilt now
			RunSearch();
		}

		private void RunSearch()
		{
			// A row builder that refreshes the search would otherwise rebuild the rows it's in the middle of
			if (m_disposed || m_searching) {
				return;
			}

			m_searching = true;

			try {
				Search();
			} finally {
				m_searching = false;
			}
		}

		private void Search()
		{
			DisposeRows();
			Results.Clear();

			if (m_scroll != null) {
				m_scroll.RestorePosition(default);
			}

			var query = m_query.Trim();

			if (query.Length == 0 && !m_showAllWhenEmpty) {
				return;
			}

			SplitWords(query);

			IEnumerable<T> items;

			try {
				items = m_getItems?.Invoke();
			} catch (Exception exception) {
				Debug.LogException(exception);
				Results.Label($"<color={ERROR_COLOR}>{exception.GetType().Name}: {exception.Message}</color>");
				return;
			}

			if (items == null) {
				return;
			}

			var shown = 0;
			var hidden = 0;

			foreach (var item in items) {
				if (query.Length > 0 && !Matches(item, query)) {
					continue;
				}

				if (shown < m_maxResults) {
					BuildRow(CreateRowContainer(), item, shown);
					shown++;
				} else {
					hidden++;
				}
			}

			if (shown == 0) {
				Results.Label(query.Length > 0 ? "No matches" : "Nothing to show");
			} else if (hidden > 0) {
				Results.Label(query.Length > 0 ? $"{hidden} more, keep typing" : $"{hidden} more, type to search");
			}
		}

		private bool Matches(T item, string query)
		{
			try {
				if (m_match != null) {
					return m_match(item, query);
				}

				var text = m_getText?.Invoke(item) ?? string.Empty;

				foreach (var word in m_words) {
					if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0) {
						return false;
					}
				}

				return true;
			} catch (Exception exception) {
				// One bad item shouldn't stop the search
				Debug.LogException(exception);
				return false;
			}
		}

		private void SplitWords(string query)
		{
			m_words.Clear();

			foreach (var word in query.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
				m_words.Add(word);
			}
		}

		private ContainerWidget CreateRowContainer()
		{
			return ContainerWidget.CreatePlain(Results.Content, "Result");
		}

		private void BuildRow(ContainerWidget container, T item, int index)
		{
			var row = new SearchRow<T>(this, container, item, m_query, index);

			if (index < m_rows.Count) {
				m_rows[index] = row;
			} else {
				m_rows.Add(row);
			}

			row.IsBuilding = true;

			try {
				m_buildRow?.Invoke(row);
			} catch (Exception exception) {
				Debug.LogException(exception);
				container.Label($"<color={ERROR_COLOR}>{exception.GetType().Name}: {exception.Message}</color>");
			} finally {
				row.IsBuilding = false;
			}
		}

		private void DisposeRows()
		{
			foreach (var row in m_rows) {
				row.Dispose();
			}

			m_rows.Clear();
		}
	}
}
