using System;
using System.Collections.Generic;
using System.Text;
using Tekly.DevBoard.Access;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Nodes;
using Tekly.DevBoard.Pages;
using Tekly.DevBoard.Panels;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.Palette
{
	/// <summary>
	/// Searches every page and control in DevBoard by name, and acts on the one picked: pages and links open in a
	/// panel, buttons run, toggles flip (and the palette stays open), and anything else is revealed in a panel:
	/// scrolled to and flashed, ready to use. Holding Shift reveals buttons, toggles and links instead of using them.
	///
	/// It's a dropdown popup over all panels, filled from NodeAccess, so opening it builds any segments that
	/// haven't been built yet. The popup does the keys: Up and Down (or Tab and Shift+Tab) move the selection,
	/// Enter picks, Escape closes.
	///
	/// A searchable list is one entry, not one per row: it has its own search, and its rows may be hidden by it.
	/// Picking it reveals the list.
	///
	/// Opening it doesn't show DevBoard: buttons and toggles can be used over the game with the panels hidden.
	/// Picks that need a panel (pages, links, reveals) show DevBoard then.
	/// </summary>
	internal sealed class CommandPalette
	{
		private const float WIDTH = 560f;
		private const float MARGIN = 16f;
		private const float TOP_FRACTION = 0.1f;
		private const float HEIGHT_FRACTION = 0.6f;
		private const string SEPARATOR = "  ·  ";
		private const string PATH_SEPARATOR = " › ";

		public bool IsOpen => m_blocker != null;

		private readonly DevBoardUI m_board;
		private readonly List<Entry> m_entries = new();
		private readonly List<(int Index, int Score)> m_scored = new();
		private readonly List<string> m_words = new();

		private PopupBlocker m_blocker;
		private DropdownPopup m_popup;

		// Brought back, selected, the next time the palette opens
		private string m_lastQuery;
		private int m_pendingPick = -1;
		private bool m_pendingReveal;
		private bool m_closeRequested;

		public CommandPalette(DevBoardUI board)
		{
			m_board = board;
		}

		public void Open()
		{
			if (IsOpen) {
				m_popup.FocusSearch();
				return;
			}

			// Popups float over the panels' canvas, which only exists once there's a panel. A panel made while
			// DevBoard is hidden stays hidden
			if (m_board.PopupLayer == null) {
				m_board.Panel(DevBoardUI.MAIN_PANEL_ID);
			}

			var layer = m_board.PopupLayer;

			if (layer == null) {
				return;
			}

			CollectEntries();

			m_blocker = PopupBlocker.Create(layer, "Command Palette", () => m_closeRequested = true);
			m_popup = UnityEngine.Object.Instantiate(m_board.Get(WidgetPrefabs.DropdownPopup), m_blocker.RectTransform, false);
			m_popup.Initialize(Choices(), -1, QueuePick, true, WidgetPrefabs.DropdownOption, Filter, m_lastQuery);
			m_popup.Cancelled += () => m_closeRequested = true;

			Place(layer);
			m_popup.FocusSearch();

			// Turns the canvas on if DevBoard is hidden
			m_board.ApplyVisibility();
		}

		public void Close()
		{
			m_closeRequested = false;
			m_pendingPick = -1;

			if (m_popup != null) {
				m_lastQuery = m_popup.Query;
				m_popup = null;
			}

			m_entries.Clear();

			if (m_blocker != null) {
				ContainerWidget.DestroyWidget(m_blocker.gameObject);
				m_blocker = null;

				// Turns the canvas back off if DevBoard is hidden
				m_board.ApplyVisibility();
			}
		}

		/// <summary>
		/// Called every frame. Picks and closes happen here rather than inside the click that asked for them,
		/// since both can destroy the option that was clicked.
		/// </summary>
		public void Update()
		{
			if (!IsOpen) {
				return;
			}

			if (m_closeRequested) {
				Close();
				return;
			}

			if (m_pendingPick >= 0) {
				var pick = m_pendingPick;
				m_pendingPick = -1;
				Pick(m_entries[pick], m_pendingReveal);
			}
		}

		/// <summary>
		/// A click or Enter picked an entry. Holding Shift asks to reveal it rather than use it.
		/// </summary>
		private void QueuePick(int index)
		{
			m_pendingPick = index;
			m_pendingReveal = KeyInput.Shift;
		}

		private void Pick(Entry entry, bool reveal)
		{
			var access = DevBoard.Instance.Access;

			switch (entry.Node) {
				case null:
					Close();
					TargetPanel()?.Open(entry.Page.Path);
					return;
				case LinkNode link when !reveal:
					Close();
					TargetPanel()?.Open(link.Target);
					return;
				case ToggleNode toggle when !reveal:
					// Stays open, showing the new value, so several toggles can be flipped in a row
					access.TryInvoke(toggle, out _);
					RefreshValues();
					m_popup.FocusSearch();
					return;
				case ButtonNode button when !reveal:
					Close();
					access.TryInvoke(button, out _);
					return;
				default:
					Close();
					RevealInPanel(entry);
					return;
			}
		}

		/// <summary>
		/// Reveals the entry's node, or failing that (it's hidden, or only for overlays) opens its page.
		/// </summary>
		private void RevealInPanel(Entry entry)
		{
			var panel = TargetPanel();

			if (panel != null && !panel.Reveal(entry.Node)) {
				panel.Open(entry.Page.Path);
			}
		}

		/// <summary>
		/// The panel most likely to be the one in use: the last interactive panel on screen, or the main panel.
		/// Shows DevBoard, since whatever asks for a panel is about to show something in it.
		/// </summary>
		private DevBoardPanel TargetPanel()
		{
			m_board.SetVisible(true);

			DevBoardPanel target = null;

			foreach (var panel in m_board.Panels) {
				if (panel != null && !panel.IsOverlay && panel.isActiveAndEnabled) {
					target = panel;
				}
			}

			target ??= m_board.Panel(DevBoardUI.MAIN_PANEL_ID);

			if (target != null) {
				target.SetCollapsed(false);
			}

			return target;
		}

		private void CollectEntries()
		{
			m_entries.Clear();

			var access = DevBoard.Instance.Access;

			foreach (var page in access.AllPages()) {
				if (!page.IsRoot) {
					AddEntry(page, null, page.Title, "page", null);
				}

				foreach (var node in access.NodesOn(page)) {
					var kind = access.KindOf(node);

					// Labels have nothing to do, hidden nodes would be confusing to find, and searchable lists are
					// searched with their own search
					if (kind == null || node is LabelNode || !NodeAccess.IsVisible(node) || IsInSearchableList(node)) {
						continue;
					}

					AddEntry(page, node, Title(node, kind), kind, ReadValue(node));
				}
			}
		}

		private void AddEntry(PageNode page, Node node, string title, string kind, string value)
		{
			var location = Location(page, node);
			var search = new StringBuilder(title).Append(' ').Append(location).Append(' ').Append(kind);

			if (node?.Description != null) {
				search.Append(' ').Append(node.Description);
			}

			if (node?.Keywords != null) {
				foreach (var keyword in node.Keywords) {
					search.Append(' ').Append(keyword);
				}
			}

			m_entries.Add(new Entry {
				Page = page,
				Node = node,
				Title = title,
				Kind = kind,
				Location = location,
				Value = value,
				SearchText = search.ToString()
			});
		}

		/// <summary>
		/// Reads every value again, e.g. after a toggle was flipped.
		/// </summary>
		private void RefreshValues()
		{
			foreach (var entry in m_entries) {
				if (entry.Node != null) {
					entry.Value = ReadValue(entry.Node);
				}
			}

			m_popup.SetChoices(Choices());
		}

		private string ReadValue(Node node)
		{
			return node is LinkNode || !DevBoard.Instance.Access.TryRead(node, out var value) ? null : value;
		}

		private DropdownChoice[] Choices()
		{
			var choices = new DropdownChoice[m_entries.Count];

			for (var i = 0; i < choices.Length; i++) {
				var entry = m_entries[i];
				var subtitle = entry.Value != null
					? $"{entry.Kind}{SEPARATOR}{entry.Value}{SEPARATOR}{entry.Location}"
					: $"{entry.Kind}{SEPARATOR}{entry.Location}";

				choices[i] = new DropdownChoice(entry.Title, subtitle);
			}

			return choices;
		}

		/// <summary>
		/// Entries containing every typed word, best first: titles that are the search, then titles starting with
		/// it, then titles holding its words, then anything else that matches.
		/// </summary>
		private void Filter(string query, List<int> matches)
		{
			query = query.Trim();

			m_words.Clear();
			m_words.AddRange(query.Split(' ', StringSplitOptions.RemoveEmptyEntries));
			m_scored.Clear();

			for (var i = 0; i < m_entries.Count; i++) {
				var entry = m_entries[i];

				if (Matches(entry.SearchText)) {
					m_scored.Add((i, Score(entry.Title, query)));
				}
			}

			// Stable, so equal scores keep page order
			m_scored.Sort((a, b) => a.Score != b.Score ? b.Score.CompareTo(a.Score) : a.Index.CompareTo(b.Index));

			foreach (var (index, _) in m_scored) {
				matches.Add(index);
			}
		}

		private bool Matches(string text)
		{
			foreach (var word in m_words) {
				if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0) {
					return false;
				}
			}

			return true;
		}

		private int Score(string title, string query)
		{
			if (query.Length == 0) {
				return 0;
			}

			if (string.Equals(title, query, StringComparison.OrdinalIgnoreCase)) {
				return 100;
			}

			var score = title.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 50 : 0;

			foreach (var word in m_words) {
				var at = title.IndexOf(word, StringComparison.OrdinalIgnoreCase);

				if (at < 0) {
					continue;
				}

				// Words matched at the start of a word in the title count for more than ones in the middle
				score += at == 0 || title[at - 1] == ' ' ? 15 : 10;
			}

			return score;
		}

		private static bool IsInSearchableList(Node node)
		{
			for (var parent = node.Parent; parent != null; parent = parent.Parent) {
				if (parent is ListNode { IsSearchable: true }) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// A node's label, or its kind without one. Inside a list row, the row's key follows, since every row has
		/// the same controls.
		/// </summary>
		private static string Title(Node node, string kind)
		{
			var title = node.Label ?? kind;

			for (var parent = node.Parent; parent != null; parent = parent.Parent) {
				if (parent is IListRow row && row.KeyText != null) {
					return $"{title}{SEPARATOR}{row.KeyText}";
				}
			}

			return title;
		}

		/// <summary>
		/// Where an entry is, e.g. "Sample › Controls › Sliders": its page, and for a node, its segment's title.
		/// </summary>
		private static string Location(PageNode page, Node node)
		{
			var builder = new StringBuilder();

			for (var current = node == null ? page.Parent : page; current != null && !current.IsRoot; current = current.Parent) {
				builder.Insert(0, builder.Length > 0 ? current.Title + PATH_SEPARATOR : current.Title);
			}

			var segment = Segment.Of(node);

			if (segment?.Title != null) {
				builder.Append(builder.Length > 0 ? PATH_SEPARATOR : string.Empty).Append(segment.Title);
			}

			return builder.Length > 0 ? builder.ToString() : PageTree.ROOT_TITLE;
		}

		/// <summary>
		/// Near the top of the screen, centered, and bounded so it fits.
		/// </summary>
		private void Place(RectTransform layer)
		{
			var layerRect = layer.rect;
			var popup = (RectTransform) m_popup.transform;

			if (popup.TryGetComponent(out LayoutItem layout)) {
				layout.MinWidth = Mathf.Min(WIDTH, layerRect.width - MARGIN * 2f);
				layout.MaxWidth = layerRect.width - MARGIN * 2f;
				layout.MaxHeight = layerRect.height * HEIGHT_FRACTION;
			}

			popup.anchorMin = popup.anchorMax = new Vector2(0.5f, 1f);
			popup.pivot = new Vector2(0.5f, 1f);
			popup.anchoredPosition = new Vector2(0f, -layerRect.height * TOP_FRACTION);
		}

		private class Entry
		{
			public PageNode Page;

			/// <summary>
			/// Null for a page.
			/// </summary>
			public Node Node;

			public string Title;
			public string Kind;
			public string Location;
			public string Value;
			public string SearchText;
		}
	}
}
