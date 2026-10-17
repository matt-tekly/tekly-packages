using System;
using System.Collections.Generic;
using System.Text;
using Tekly.Common.Search;
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
	/// Matching is loose (see SearchQuery), and recent picks come first among
	/// equally good matches, so an empty search starts with them.
	///
	/// Chips under the search narrow it to a category in plain words: Pages (pages, their sections and links),
	/// Actions (things that run), Settings (things you change) and Info (values to read). While searching, each
	/// shows how many entries match, so it's clear where the matches are before picking one.
	///
	/// It's for finding things on the board, for anyone. Typed commands are the console's, see DevConsole.
	///
	/// With nothing typed and no category picked it's a menu: recent picks, then every page, top level first.
	///
	/// A searchable list is one entry, not one per row: it has its own search, and its rows may be hidden by it.
	/// Picking it reveals the list.
	///
	/// Opening it doesn't show DevBoard: buttons and toggles can be used over the game with the panels hidden.
	/// Picks that need a panel (pages, links, reveals) use one already on screen, like a pinned one, and only
	/// show DevBoard when there isn't one.
	/// </summary>
	internal sealed class CommandPalette
	{
		private const float WIDTH = 560f;
		private const float MARGIN = 16f;
		private const float TOP_FRACTION = 0.1f;
		private const float HEIGHT_FRACTION = 0.6f;
		private const string SEPARATOR = "  ·  ";
		private const string PATH_SEPARATOR = " › ";
		private const int MAX_RECENT = 8;

		private const string PAGE_KIND = "page";
		private const string SECTION_KIND = "section";
		private const string RECENT = "recent";

		// Shown as the value of a button that isn't interactable
		private const string DISABLED_VALUE = "Disabled";

		// Rich text for the chips: the picked one in the accent color, counts dimmed
		private const string CHIP_SELECTED = "<color=#F6C050>{0}</color>";
		private const string CHIP_COUNT = "{0} <alpha=#80>{1}<alpha=#FF>";

		private static readonly Category[] s_categories = (Category[]) Enum.GetValues(typeof(Category));
		private static readonly int s_categoryCount = s_categories.Length;

		public bool IsOpen => m_blocker != null;

		private readonly DevBoardUI m_board;
		private readonly List<Entry> m_entries = new();
		private readonly List<(int Index, int Score, int Recent)> m_scored = new();
		private readonly SearchQuery m_query = new();
		private readonly int[] m_categoryCounts = new int[s_categoryCount];
		private readonly ButtonWidget[] m_chips = new ButtonWidget[s_categoryCount];

		// Keys of the most recently picked entries, most recent first
		private readonly List<string> m_recent = new();

		private PopupBlocker m_blocker;
		private DropdownPopup m_popup;

		// Brought back, selected, the next time the palette opens
		private string m_lastQuery;
		private Category m_category;
		private int m_pendingPick = -1;
		private bool m_pendingReveal;
		private bool m_closeRequested;

		// Puts the keyboard back in the search a frame after a pick that keeps the palette open, see RefocusSearch
		private int m_refocusAfterFrame = -1;
		private bool m_refocusSelectAll;

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
			AddChips();

			Place(layer);
			m_popup.FocusSearch();

			// Turns the canvas on if DevBoard is hidden
			m_board.ApplyVisibility();
		}

		public void Close()
		{
			m_closeRequested = false;
			m_pendingPick = -1;
			m_refocusAfterFrame = -1;

			if (m_popup != null) {
				m_lastQuery = m_popup.Query;
				m_popup = null;
			}

			m_entries.Clear();
			Array.Clear(m_chips, 0, m_chips.Length);

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

			if (m_refocusAfterFrame >= 0 && Time.frameCount > m_refocusAfterFrame) {
				m_refocusAfterFrame = -1;
				m_popup.FocusSearch(m_refocusSelectAll);
			}
		}

		/// <summary>
		/// Puts the keyboard back in the search next frame. Enter deactivates the field later in the frame it's
		/// pressed, after the pick it caused, so focusing straight away would be undone.
		/// </summary>
		private void RefocusSearch(bool selectAll)
		{
			m_refocusAfterFrame = Time.frameCount;
			m_refocusSelectAll = selectAll;
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

			Remember(entry);

			// A disabled button or toggle does nothing, as it would in a panel, and the palette stays open. Other
			// controls are revealed in a panel either way, where they show as disabled
			if (!reveal && entry.Node is ButtonNode or ToggleNode && !entry.Node.IsInteractable()) {
				RefreshValues();
				RefocusSearch(false);
				return;
			}

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
					RefocusSearch(true);
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
		/// Moves the entry to the front of the recent picks.
		/// </summary>
		private void Remember(Entry entry)
		{
			if (entry.Key == null) {
				return;
			}

			m_recent.Remove(entry.Key);
			m_recent.Insert(0, entry.Key);

			if (m_recent.Count > MAX_RECENT) {
				m_recent.RemoveAt(m_recent.Count - 1);
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
		/// The panel to show a pick in, from those already on screen: the one last clicked or touched, else the last
		/// interactive one, else the last overlay. Panels pinned to show while DevBoard is hidden count, so using one
		/// doesn't bring the rest of DevBoard back. With none on screen, shows DevBoard and picks the same way, or
		/// makes a panel if there are none at all.
		/// </summary>
		private DevBoardPanel TargetPanel()
		{
			var target = ShownPanel();

			if (target == null) {
				m_board.SetVisible(true);
				target = ShownPanel() ?? m_board.CreateRootPanel();
			}

			if (target != null) {
				target.SetCollapsed(false);
			}

			return target;
		}

		private DevBoardPanel ShownPanel()
		{
			return m_board.LastUsedPanel ?? LastShownPanel(false) ?? LastShownPanel(true);
		}

		/// <summary>
		/// The last panel on screen in the given mode, or null if there isn't one.
		/// </summary>
		private DevBoardPanel LastShownPanel(bool overlay)
		{
			DevBoardPanel shown = null;

			foreach (var panel in m_board.Panels) {
				if (panel != null && panel.IsOverlay == overlay && panel.isActiveAndEnabled) {
					shown = panel;
				}
			}

			return shown;
		}

		private void CollectEntries()
		{
			m_entries.Clear();

			var access = DevBoard.Instance.Access;

			foreach (var page in access.AllPages()) {
				if (!page.IsRoot) {
					AddEntry(page, null, page.Title, page.Title, PAGE_KIND, null, Category.Pages);
				}

				foreach (var node in access.NodesOn(page)) {
					// Titled segments, foldouts and tabs are sections to jump to
					if (node is Segment { Title: not null } segment) {
						AddEntry(page, node, segment.Title, segment.Title, SECTION_KIND, null, Category.Pages);
						continue;
					}

					var kind = node is FoldoutNode { Label: not null } or TabNode { Label: not null } ? SECTION_KIND : access.KindOf(node);

					// Labels have nothing to do, hidden nodes would be confusing to find, and rows of searchable and
					// virtual lists are found with the list's own search
					if (kind == null || node is LabelNode || !NodeAccess.IsVisible(node) || IsInListWithOwnSearch(node)) {
						continue;
					}

					AddEntry(page, node, Title(node, kind), node.Label ?? kind, kind, ReadValue(node), CategoryOf(node, kind));
				}
			}
		}

		/// <summary>
		/// Which chip a control is under, from what it can do, so custom nodes registered with NodeAccess land
		/// somewhere sensible too.
		/// </summary>
		private static Category CategoryOf(Node node, string kind)
		{
			var access = DevBoard.Instance.Access;

			if (kind == SECTION_KIND || node is LinkNode) {
				return Category.Pages;
			}

			if (access.CanWrite(node)) {
				return Category.Settings;
			}

			return access.CanInvoke(node) ? Category.Actions : Category.Info;
		}

		private void AddEntry(PageNode page, Node node, string title, string name, string kind, string value, Category category)
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
				Key = node != null ? node.Path : "page:" + page.Path,
				Title = title,
				Name = name,
				Kind = kind,
				Category = category,
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
			if (node is ButtonNode button) {
				return button.IsInteractable() ? null : DISABLED_VALUE;
			}

			if (!node.IsInteractable() && DevBoard.Instance.Access.TryRead(node, out var disabledValue)) {
				return $"{disabledValue} ({DISABLED_VALUE})";
			}

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

				if (RecentRank(entry) != int.MaxValue) {
					subtitle = RECENT + SEPARATOR + subtitle;
				}

				choices[i] = new DropdownChoice(entry.Title, subtitle);
			}

			return choices;
		}

		/// <summary>
		/// Entries matching the search and the picked category, best first (see <see cref="SearchQuery"/>), scored
		/// on their name, with their location, kind, row key, description and keywords as other text. Ties go to
		/// recent picks, then page order. Counts every category's matches for the chips as it goes.
		///
		/// With nothing typed and no category it's the menu: recent picks, then pages, top level first.
		/// </summary>
		private void Filter(string query, List<int> matches)
		{
			m_query.Set(query);
			m_scored.Clear();
			Array.Clear(m_categoryCounts, 0, m_categoryCounts.Length);

			var isMenu = m_query.IsEmpty && m_category == Category.All;

			for (var i = 0; i < m_entries.Count; i++) {
				var entry = m_entries[i];
				var score = m_query.Score(entry.Name, entry.SearchText);

				if (score == SearchQuery.NO_MATCH) {
					continue;
				}

				m_categoryCounts[(int) Category.All]++;
				m_categoryCounts[(int) entry.Category]++;

				if (m_category != Category.All && entry.Category != m_category) {
					continue;
				}

				var recent = RecentRank(entry);

				if (isMenu) {
					var isPage = entry.Node == null;

					if (recent == int.MaxValue && !isPage) {
						continue;
					}

					// Recent picks first, then pages by depth
					score = recent != int.MaxValue ? 1 : -Depth(entry.Page);
				}

				m_scored.Add((i, score, recent));
			}

			UpdateChips();

			m_scored.Sort((a, b) => {
				if (a.Score != b.Score) {
					return b.Score.CompareTo(a.Score);
				}

				return a.Recent != b.Recent ? a.Recent.CompareTo(b.Recent) : a.Index.CompareTo(b.Index);
			});

			foreach (var scored in m_scored) {
				matches.Add(scored.Index);
			}
		}

		private static int Depth(PageNode page)
		{
			var depth = 0;

			for (var current = page; current != null && !current.IsRoot; current = current.Parent) {
				depth++;
			}

			return depth;
		}

		/// <summary>
		/// A chip per category, in a row between the search and the list.
		/// </summary>
		private void AddChips()
		{
			// Tight, so all the chips fit the palette's width
			var row = m_popup.AddHeader().WithPadding(0).WithSpacing(2);

			foreach (var category in s_categories) {
				m_chips[(int) category] = row.Button(category.ToString(), () => PickCategory(category));
			}

			UpdateChips();
		}

		/// <summary>
		/// Picking the chip already picked goes back to All.
		/// </summary>
		private void PickCategory(Category category)
		{
			m_category = category == m_category ? Category.All : category;
			m_popup.Refilter();

			// Back to typing, without selecting the text so the next key doesn't replace it
			m_popup.FocusSearch(false);
		}

		private void UpdateChips()
		{
			foreach (var category in s_categories) {
				var chip = m_chips[(int) category];

				if (chip == null) {
					continue;
				}

				// Counts only mean something while searching, and leaving them off otherwise keeps the chips short
				var label = category == Category.All || m_query.IsEmpty
					? category.ToString()
					: string.Format(CHIP_COUNT, category, m_categoryCounts[(int) category]);

				chip.Label = category == m_category ? string.Format(CHIP_SELECTED, label) : label;
			}
		}

		/// <summary>
		/// Where the entry is in the recent picks, or past the end if it isn't one.
		/// </summary>
		private int RecentRank(Entry entry)
		{
			var rank = entry.Key != null ? m_recent.IndexOf(entry.Key) : -1;
			return rank >= 0 ? rank : int.MaxValue;
		}

		private static bool IsInListWithOwnSearch(Node node)
		{
			for (var parent = node.Parent; parent != null; parent = parent.Parent) {
				// Virtual lists only build rows as they're shown, so their rows would come and go
				if (parent is ListNode { IsSearchable: true } or ListNode { IsVirtual: true }) {
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

			// A segment's own entry is titled with it already
			if (segment?.Title != null && segment != node) {
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

		private enum Category
		{
			All,
			Pages,
			Actions,
			Settings,
			Info
		}

		private class Entry
		{
			public PageNode Page;

			/// <summary>
			/// Null for a page.
			/// </summary>
			public Node Node;

			/// <summary>
			/// Identifies the entry across openings, for recent picks: the node's path, or the page's.
			/// </summary>
			public string Key;

			public string Title;

			/// <summary>
			/// What searches are scored against: the title without a list row's key.
			/// </summary>
			public string Name;

			public string Kind;
			public Category Category;
			public string Location;
			public string Value;
			public string SearchText;
		}
	}
}
