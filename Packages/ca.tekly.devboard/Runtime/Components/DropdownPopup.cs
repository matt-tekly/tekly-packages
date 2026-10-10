using System;
using System.Collections.Generic;
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
	/// </summary>
	public class DropdownPopup : Widget
	{
		[Tooltip("Shown only for searchable dropdowns. Typing hides the choices that don't match")]
		[SerializeField] private TextInputWidget m_search;
		[SerializeField] private ScrollViewWidget m_list;

		// Rows beyond the ones in view, so a fast scroll doesn't show a gap before the next refresh
		private const int EXTRA_ROWS = 2;

		private readonly List<int> m_matches = new();
		private readonly List<Row> m_rows = new();

		private DropdownChoice[] m_choices;
		private int m_selectedIndex = -1;
		private Action<int> m_onSelect;
		private string m_optionVariant;
		private string m_query = string.Empty;

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
		/// Shows the choices with options built from optionVariant, marking selectedIndex as the current value
		/// (-1 for none). onSelect gets the index of the one clicked.
		/// </summary>
		public void Initialize(DropdownChoice[] choices, int selectedIndex, Action<int> onSelect, bool searchable, string optionVariant)
		{
			m_choices = choices;
			m_selectedIndex = selectedIndex;
			m_onSelect = onSelect;
			m_optionVariant = optionVariant;

			m_list.WithoutSavedState();
			m_scrollRect = m_list.GetComponent<ScrollRect>();
			m_viewport = (RectTransform) m_list.Content.parent;
			m_contentLayout = m_list.Content.GetComponent<FlowLayout>();

			if (m_search != null) {
				m_search.gameObject.SetActive(searchable);

				if (searchable) {
					m_search.Initialize(null, "Search", () => m_query, SetQuery, InputMode.Immediate);
				}
			}

			MeasureChoices();
			SetQuery(string.Empty);
			ScrollToSelected();
		}

		private void LateUpdate()
		{
			if (m_rowHeight > 0f) {
				ShowRowsInView();
			}
		}

		private void SetQuery(string query)
		{
			m_query = query ?? string.Empty;
			m_matches.Clear();

			for (var i = 0; i < m_choices.Length; i++) {
				if (Matches(m_choices[i], m_query)) {
					m_matches.Add(i);
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

		private static bool Matches(in DropdownChoice choice, string query)
		{
			return query.Length == 0
				|| Contains(choice.Title, query)
				|| choice.HasSubtitle && Contains(choice.Subtitle, query);
		}

		private static bool Contains(string text, string query)
		{
			return text != null && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
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
			var row = new Row { Option = m_list.Create<DropdownOption>(m_optionVariant) };
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
