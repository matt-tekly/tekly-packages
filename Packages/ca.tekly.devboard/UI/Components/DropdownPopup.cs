using System;
using System.Collections.Generic;
using Tekly.Common.Search;
using Tekly.DevBoard.Components.Inputs;
using Tekly.Trellis;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// The list a DropdownWidget opens: an optional search field over a scrolling list of choices. The prefab
	/// lays itself out (the root fits its content, and the list can shrink and scroll); the dropdown only places
	/// it and bounds its size.
	///
	/// The list is virtualized, so it costs the same for ten choices or ten thousand. Rows are DropdownOptions,
	/// which can measure a choice without showing it: the popup measures every choice once, gives every row the
	/// tallest height, and the list the widest width. Only the rows in view exist, from a small pool that's
	/// repositioned and re-shown as the list scrolls. The content's layout is told the full list's height.
	///
	/// The selected choice is shown with the option's On state. While the search field is being typed in, Up and
	/// Down (or Tab and Shift+Tab) move the selection and Enter picks it, so the list can be used without leaving
	/// the field. Escape raises Cancelled. A search that hides the selection moves it to the first match, and with
	/// a custom filter the selection always follows the best match.
	/// </summary>
	public class DropdownPopup : Widget
	{
		[Tooltip("Shown only for searchable dropdowns. Typing hides the choices that don't match")]
		[SerializeField] private TextInputWidget m_search;
		[SerializeField] private ScrollViewWidget m_list;

		// Rows beyond the ones in view, so a fast scroll doesn't show a gap before the next refresh
		private const int EXTRA_ROWS = 2;

		private readonly List<int> m_matches = new();
		private readonly List<(int Index, int Score)> m_scored = new();
		private readonly SearchQuery m_searchQuery = new();
		private readonly List<Row> m_rows = new();

		private DropdownChoice[] m_choices;
		private int m_selectedIndex = -1;
		private Action<int> m_onSelect;
		private Action<string, List<int>> m_filter;
		private WidgetPrefab<DropdownOption> m_optionPrefab;
		private string m_query = string.Empty;
		private bool m_wasSearching;

		private ScrollRect m_scrollRect;
		private RectTransform m_viewport;
		private FlowLayout m_contentLayout;

		private float m_rowHeight;
		private int m_firstShown = -1;
		private int m_shownCount = -1;

		private class Row
		{
			public DropdownOption Option;
			public int Item = -1;
		}

		/// <summary>
		/// The search text.
		/// </summary>
		public string Query => m_query;

		/// <summary>
		/// Escape was pressed. Whoever opened the popup closes it.
		/// </summary>
		public event Action Cancelled;

		/// <summary>
		/// The index of the selected choice, -1 for none.
		/// </summary>
		public int SelectedIndex => m_selectedIndex;

		/// <summary>
		/// Shows the choices with options built from optionPrefab, marking selectedIndex as the current value
		/// (-1 for none). onSelect gets the index of the one clicked.
		///
		/// filter, when given, replaces the search: it gets the search text and fills the list with the indexes of
		/// the choices to show, in the order to show them. The selection then follows the first one.
		///
		/// query is the search text to start with, e.g. the last one used.
		/// </summary>
		public void Initialize(DropdownChoice[] choices, int selectedIndex, Action<int> onSelect, bool searchable, WidgetPrefab<DropdownOption> optionPrefab,
			Action<string, List<int>> filter = null, string query = null)
		{
			m_query = searchable ? query ?? string.Empty : string.Empty;
			m_choices = choices;
			m_selectedIndex = selectedIndex;
			m_onSelect = onSelect;
			m_optionPrefab = optionPrefab;
			m_filter = filter;

			m_scrollRect = m_list.GetComponent<ScrollRect>();
			m_viewport = (RectTransform) m_list.Content.parent;
			m_contentLayout = m_list.Content.GetComponent<FlowLayout>();

			if (m_search != null) {
				m_search.gameObject.SetActive(searchable);

				if (searchable) {
					m_search.Initialize(null, new Binding<string>(() => m_query, OnQueryTyped));
					m_search.WithPlaceholder("Search").WithInputMode(InputMode.Immediate);

					// Escape closes the popup, so it shouldn't also throw away what was typed
					m_search.InputField.restoreOriginalTextOnEscape = false;

					var tabHandler = m_search.InputField.GetComponent<SearchTabHandler>();

					if (tabHandler == null) {
						tabHandler = m_search.InputField.gameObject.AddComponent<SearchTabHandler>();
					}

					tabHandler.Tabbed = OnSearchTabbed;
				}
			}

			MeasureChoices();
			Filter(m_query);
			ScrollToSelected();
		}

		/// <summary>
		/// Moves the selection through the choices shown, e.g. -1 for the arrow up key.
		/// </summary>
		public void MoveSelection(int delta)
		{
			if (m_matches.Count == 0) {
				return;
			}

			var position = m_matches.IndexOf(m_selectedIndex);

			if (position < 0) {
				position = delta > 0 ? -1 : m_matches.Count;
			}

			position = Mathf.Clamp(position + delta, 0, m_matches.Count - 1);
			m_selectedIndex = m_matches[position];

			ScrollIntoView(position);
			ShowSelection();
		}

		/// <summary>
		/// Does what clicking the selected choice does.
		/// </summary>
		public void ActivateSelected()
		{
			if (m_selectedIndex >= 0) {
				m_onSelect(m_selectedIndex);
			}
		}

		/// <summary>
		/// Replaces the choices, e.g. after their text changed, keeping the search, selection and scroll position.
		/// Options aren't measured again, so the choices should be about the same size.
		/// </summary>
		public void SetChoices(DropdownChoice[] choices)
		{
			var scroll = m_list.Content.anchoredPosition;
			var selected = m_selectedIndex;

			m_choices = choices;
			Filter(m_query);

			if (selected >= 0 && selected < choices.Length && m_matches.Contains(selected)) {
				m_selectedIndex = selected;
			}

			m_list.Content.anchoredPosition = scroll;

			foreach (var row in m_rows) {
				row.Item = -1;
			}

			m_firstShown = -1;
			ShowRowsInView();
			ShowSelection();
		}

		/// <summary>
		/// Puts the keyboard in the search field, if there is one. With selectAll, its text is selected so typing
		/// replaces it.
		/// </summary>
		public void FocusSearch(bool selectAll = true)
		{
			if (m_search != null && m_search.gameObject.activeInHierarchy) {
				m_search.Focus(selectAll);
			}
		}

		private void Update()
		{
			if (KeyInput.Escape) {
				Cancelled?.Invoke();
				return;
			}

			// The field stops being focused in the same frame Enter is pressed, so last frame counts too
			var isSearching = m_search != null && m_search.isActiveAndEnabled && m_search.IsFocused;
			var wasSearching = m_wasSearching;
			m_wasSearching = isSearching;

			if (!isSearching && !wasSearching) {
				return;
			}

			if (KeyInput.Up) {
				MoveSelection(-1);
			} else if (KeyInput.Down) {
				MoveSelection(1);
			} else if (KeyInput.Enter) {
				ActivateSelected();
			}
		}

		private void OnSearchTabbed(bool isReverse)
		{
			MoveSelection(isReverse ? -1 : 1);
		}

		private void LateUpdate()
		{
			if (m_rowHeight > 0f) {
				ShowRowsInView();
			}
		}

		/// <summary>
		/// From the search field. It also writes back unchanged text, e.g. when Enter ends editing, and filtering
		/// again then would move a selection made with the arrow keys back to the first match.
		/// </summary>
		private void OnQueryTyped(string query)
		{
			query ??= string.Empty;

			if (query != m_query) {
				Filter(query);
			}
		}

		/// <summary>
		/// Adds a row between the search field and the list, below any added before, for controls that change
		/// what's listed, like the command palette's category chips. Call Refilter when they change it.
		/// </summary>
		public ContainerWidget AddHeader()
		{
			var row = ContainerWidget.CreatePlainRow(m_list.transform.parent, "Header");
			row.transform.SetSiblingIndex(m_list.transform.GetSiblingIndex());

			return row;
		}

		/// <summary>
		/// Runs the search again, for when what a custom filter keeps has changed.
		/// </summary>
		public void Refilter()
		{
			Filter(m_query);
			ShowSelection();
		}

		/// <summary>
		/// Shows the choices matching query, from the top of the list.
		/// </summary>
		private void Filter(string query)
		{
			m_query = query ?? string.Empty;
			m_matches.Clear();

			if (m_filter != null) {
				m_filter(m_query, m_matches);
				m_selectedIndex = m_matches.Count > 0 ? m_matches[0] : -1;
			} else {
				MatchChoices();

				// The current choice stays selected while it shows; otherwise Enter picks the first match
				if (m_query.Length > 0 && !m_matches.Contains(m_selectedIndex)) {
					m_selectedIndex = m_matches.Count > 0 ? m_matches[0] : -1;
				}
			}

			// The content is as tall as every match, though only the rows in view are built
			var spacing = m_contentLayout.Spacing;
			var height = m_contentLayout.Padding.Vertical + m_matches.Count * (m_rowHeight + spacing) - (m_matches.Count > 0 ? spacing : 0f);
			m_contentLayout.PreferredHeight = height;

			m_list.Content.anchoredPosition = new Vector2(m_list.Content.anchoredPosition.x, 0f);
			m_scrollRect.StopMovement();

			// Force the rows to refresh even if the same range is in view
			m_firstShown = -1;
			ShowRowsInView();
			ShowSelection();
		}

		/// <summary>
		/// Marks the selected choice on the rows in view.
		/// </summary>
		private void ShowSelection()
		{
			foreach (var row in m_rows) {
				if (row.Item >= 0 && row.Option.gameObject.activeSelf) {
					row.Option.SetSelected(row.Item == m_selectedIndex);
				}
			}
		}

		/// <summary>
		/// Scrolls just enough to show the match at position.
		/// </summary>
		private void ScrollIntoView(int position)
		{
			var stride = m_rowHeight + m_contentLayout.Spacing;
			var top = m_contentLayout.Padding.Top + position * stride;
			var bottom = top + m_rowHeight;

			var scrolled = m_list.Content.anchoredPosition.y;
			var height = m_viewport.rect.height;

			if (top < scrolled) {
				scrolled = top;
			} else if (bottom > scrolled + height) {
				scrolled = bottom - height;
			}

			m_list.Content.anchoredPosition = new Vector2(m_list.Content.anchoredPosition.x, scrolled);
			m_scrollRect.StopMovement();
			ShowRowsInView();
		}

		/// <summary>
		/// Scrolls the current value to the top of the list, one row down so the choice before it shows too.
		/// The ScrollRect clamps this when the selected choice is near the end.
		/// </summary>
		private void ScrollToSelected()
		{
			var position = m_matches.IndexOf(m_selectedIndex);

			if (position <= 0) {
				return;
			}

			var stride = m_rowHeight + m_contentLayout.Spacing;
			m_list.Content.anchoredPosition = new Vector2(m_list.Content.anchoredPosition.x, (position - 1) * stride);

			m_firstShown = -1;
			ShowRowsInView();
		}

		/// <summary>
		/// The choices matching the search (see <see cref="SearchQuery"/>), scored on their titles with subtitles as
		/// other text, best first and otherwise in order.
		/// </summary>
		private void MatchChoices()
		{
			m_searchQuery.Set(m_query);
			m_scored.Clear();

			for (var i = 0; i < m_choices.Length; i++) {
				var choice = m_choices[i];
				var score = m_searchQuery.Score(choice.Title, choice.HasSubtitle ? choice.Subtitle : null);

				if (score != SearchQuery.NO_MATCH) {
					m_scored.Add((i, score));
				}
			}

			m_scored.Sort((a, b) => a.Score != b.Score ? b.Score.CompareTo(a.Score) : a.Index.CompareTo(b.Index));

			foreach (var scored in m_scored) {
				m_matches.Add(scored.Index);
			}
		}

		/// <summary>
		/// Measures every choice with the first row: the tallest sets the row height, the widest the list's width.
		/// </summary>
		private void MeasureChoices()
		{
			var option = AddRow().Option;
			var size = option.Measure(new DropdownChoice(string.Empty));

			foreach (var choice in m_choices) {
				size = Vector2.Max(size, option.Measure(choice));
			}

			m_rowHeight = Mathf.Max(1f, size.y);

			// The list reports the widest choice, plus the content padding and the frame around the viewport
			if (m_list.TryGetComponent(out LayoutItem listLayout)) {
				var frame = Mathf.Max(0f, ((RectTransform) m_list.transform).rect.width - m_viewport.rect.width);
				listLayout.PreferredWidth = size.x + m_contentLayout.Padding.Horizontal + frame;
			}

			StretchRow((RectTransform) option.transform);
		}

		/// <summary>
		/// Makes sure there are rows for the visible part of the list, and points them at the matches in view.
		/// </summary>
		private void ShowRowsInView()
		{
			var stride = m_rowHeight + m_contentLayout.Spacing;
			var scrolled = Mathf.Max(0f, m_list.Content.anchoredPosition.y - m_contentLayout.Padding.Top);

			var first = Mathf.Clamp(Mathf.FloorToInt(scrolled / stride), 0, Mathf.Max(0, m_matches.Count - 1));
			var visible = Mathf.CeilToInt(m_viewport.rect.height / stride) + EXTRA_ROWS;
			var count = Mathf.Clamp(m_matches.Count - first, 0, visible);

			if (first == m_firstShown && count == m_shownCount) {
				return;
			}

			m_firstShown = first;
			m_shownCount = count;

			while (m_rows.Count < count) {
				StretchRow((RectTransform) AddRow().Option.transform);
			}

			for (var r = 0; r < m_rows.Count; r++) {
				var row = m_rows[r];
				var shown = r < count;

				if (row.Option.gameObject.activeSelf != shown) {
					row.Option.gameObject.SetActive(shown);
				}

				if (!shown) {
					continue;
				}

				var position = first + r;
				var item = m_matches[position];

				if (row.Item != item) {
					row.Item = item;
					row.Option.Show(m_choices[item]);
					row.Option.SetSelected(item == m_selectedIndex);
				}

				var rect = (RectTransform) row.Option.transform;
				rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -(m_contentLayout.Padding.Top + position * stride));
			}
		}

		private Row AddRow()
		{
			var row = new Row { Option = m_list.Create(m_optionPrefab) };
			row.Option.WithAction(() => {
				if (row.Item >= 0) {
					m_onSelect(row.Item);
				}
			});

			// Placed by hand, so the content's layout leaves it alone
			if (row.Option.TryGetComponent(out LayoutItem item)) {
				item.IgnoreLayout = true;
			}

			// Sized by the popup. Fitting its own size would resize it around its pivot, e.g. centering a narrow
			// option in the list
			if (row.Option.TryGetComponent(out LayoutContainer layout)) {
				layout.FitWidth = FitMode.None;
				layout.FitHeight = FitMode.None;
			}

			m_rows.Add(row);
			return row;
		}

		/// <summary>
		/// Stretches a row across the content, at the shared row height.
		/// </summary>
		private void StretchRow(RectTransform rect)
		{
			var padding = m_contentLayout.Padding;

			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(1f, 1f);
			rect.pivot = new Vector2(0.5f, 1f);
			rect.offsetMin = new Vector2(padding.Left, rect.offsetMin.y);
			rect.offsetMax = new Vector2(-padding.Right, rect.offsetMax.y);
			rect.sizeDelta = new Vector2(rect.sizeDelta.x, m_rowHeight);
		}
	}
}
