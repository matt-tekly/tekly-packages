using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using Tekly.DevBoard.Nodes;
using Tekly.Trellis;

namespace Tekly.DevBoard.Views
{
	/// <summary>
	/// One panel's widgets for a list. A searchable list adds a search field and only creates widgets for rows the
	/// search shows, up to the list's MaxResults; rows it hides keep their widgets, just switched off. The search
	/// text is this panel's own, kept in DevBoard.State.
	/// </summary>
	internal sealed class ListView : ContainerView
	{
		private readonly ListNode m_list;
		private readonly List<string> m_words = new();

		private readonly string m_stateKey;
		private readonly ScrollViewWidget m_scroll;
		private readonly LabelWidget m_footer;

		private string m_query = string.Empty;
		private bool m_matchFaulted;

		public ListView(ListNode list, ContainerWidget widget, ViewContext view) : base(list, widget, null, view)
		{
			m_list = list;

			if (!list.IsSearchable) {
				widget.WithSpacing(list.RowSpacing);
				SetTarget(widget);
				return;
			}

			m_stateKey = view.StateKey(list, "search");

			if (DevBoard.Instance.State.TryGet(m_stateKey, out string saved)) {
				m_query = saved ?? string.Empty;
			}

			widget.TextInput(null, () => m_query, SetQuery, WidgetPrefabs.SearchInput)
				.WithPlaceholder(list.Placeholder)
				.WithInputMode(InputMode.Debounced);

			ContainerWidget results;

			if (list.ScrollHeight is float maxHeight) {
				// The frame caps the height; the scroll view inside it shrinks to fit and scrolls the rest
				var frame = ContainerWidget.CreatePlain(widget.Content, "Results");
				frame.WithMaxHeight(maxHeight);

				m_scroll = frame.ScrollView().WithoutSavedState();

				// Rows are a vertical list whatever the scroll view prefab's content is set up as
				m_scroll.WithAxis(Common.Utils.LayoutAxis.Vertical)
					.WithCrossAlignment(CrossAlignment.Stretch)
					.WithSpacing(list.RowSpacing);

				results = m_scroll;
			} else {
				results = ContainerWidget.CreatePlain(widget.Content, "Results", list.RowSpacing);
			}

			m_footer = widget.Label(string.Empty);
			SetTarget(results);
		}

		protected override void SyncChildren()
		{
			if (!m_list.IsSearchable) {
				base.SyncChildren();
				return;
			}

			RemoveStaleViews();
			Filter();
			PlaceViews();
		}

		private void SetQuery(string query)
		{
			query ??= string.Empty;

			if (query == m_query) {
				return;
			}

			m_query = query;
			DevBoard.Instance.State.Set(m_stateKey, m_query);

			SyncChildren();

			// Each new search starts at the top
			m_scroll?.RestorePosition(default);
		}

		private void Filter()
		{
			var query = m_query.Trim();

			m_words.Clear();
			m_words.AddRange(query.Split(' ', StringSplitOptions.RemoveEmptyEntries));

			var shown = 0;
			var hidden = 0;

			foreach (var row in Container.Children) {
				if (!row.ShowsIn(View.IsOverlay)) {
					continue;
				}

				var matches = query.Length == 0 ? m_list.ShowAllWhenEmpty : Matches(row, query);
				Views.TryGetValue(row, out var view);

				if (matches && shown < m_list.MaxResults) {
					view ??= CreateView(row);
					view.FilteredOut = false;
					shown++;
				} else {
					if (matches) {
						hidden++;
					}

					if (view != null) {
						view.FilteredOut = true;
					}
				}
			}

			var footer = string.Empty;

			if (shown == 0 && (query.Length > 0 || m_list.ShowAllWhenEmpty)) {
				footer = query.Length > 0 ? "No matches" : "Nothing to show";
			} else if (hidden > 0) {
				footer = query.Length > 0 ? $"{hidden} more, keep typing" : $"{hidden} more, type to search";
			}

			m_footer.Text = footer;
			m_footer.gameObject.SetActive(footer.Length > 0);
		}

		private bool Matches(Node row, string query)
		{
			try {
				var matches = m_list.Matches(row, query, m_words);
				m_matchFaulted = false;

				return matches;
			} catch (Exception exception) {
				// One bad item shouldn't stop the search
				ReportFault(ref m_matchFaulted, exception);
				return false;
			}
		}
	}
}
