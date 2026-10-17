using System;
using System.Collections.Generic;
using Tekly.Common.Search;
using Tekly.DevBoard.Nodes;

namespace Tekly.DevBoard.Views
{
	/// <summary>
	/// One host's view of a virtual list. The search is matched against every item without building rows; only the
	/// matches on screen get rows built and elements made, and rows that scroll away lose their elements again (the
	/// rows themselves stay built). The search text is the host's own, kept in DevBoard.State like ListMirror's.
	/// </summary>
	public sealed class VirtualListMirror : ContainerMirror
	{
		/// <summary>
		/// The host's search text.
		/// </summary>
		public string Query { get; private set; } = string.Empty;

		private readonly ListNode m_list;
		private readonly IVirtualListElement m_listElement;
		private readonly SearchQuery m_query = new();

		// Indexes of the matching items, in order
		private readonly List<int> m_matches = new();

		private readonly HashSet<Node> m_wanted = new();
		private readonly List<Node> m_stale = new();
		private readonly List<IViewElement> m_rows = new();

		private string m_stateKey;
		private int m_matchedVersion = -1;
		private bool m_rematch;
		private bool m_refreshing;
		private bool m_matchFaulted;

		// What the element was last told to show
		private int m_shownFirst = -1;
		private int m_shownEnd = -1;
		private int m_shownCount = -1;
		private bool m_rowsChanged;

		internal VirtualListMirror(ListNode list, IVirtualListElement element, ViewContext context) : base(list, element, context)
		{
			m_list = list;
			m_listElement = element;
		}

		/// <summary>
		/// Runs a new search. Remembered for the host.
		/// </summary>
		public void SetQuery(string query)
		{
			query ??= string.Empty;

			if (query == Query) {
				return;
			}

			Query = query;
			DevBoard.Instance.State.Set(m_stateKey, Query);

			m_rematch = true;
			m_listElement.ScrollToStart();
			Refresh();
		}

		public override void Tick()
		{
			base.Tick();

			// Scrolling changes which rows are on screen, so check every refresh
			if (IsShown && ContainerElement.ChildrenVisible) {
				Refresh();
			}
		}

		protected override void Start()
		{
			if (m_list.IsSearchable) {
				m_stateKey = Context.StateKey(m_list, "search");

				if (DevBoard.Instance.State.TryGet(m_stateKey, out string saved)) {
					Query = saved ?? string.Empty;
				}

				m_listElement.ShowSearch(new Binding<string>(() => Query, SetQuery), m_list.Placeholder);
			}

			base.Start();
		}

		/// <summary>
		/// Rows were built or removed. Drops the elements of removed rows; Refresh places the rest. Building a row
		/// from inside Refresh lands here too, which is left to that Refresh.
		/// </summary>
		protected override void SyncChildren()
		{
			var count = Children.Count;
			RemoveStaleChildren();
			m_rowsChanged |= Children.Count != count;

			if (!m_refreshing) {
				Refresh();
			}
		}

		/// <summary>
		/// Scrolls to the row and shows it. Null if the host's search is hiding it.
		/// </summary>
		protected override NodeMirror RevealChild(Node child)
		{
			var index = m_list.IndexOfRow(child);

			if (index < 0) {
				return null;
			}

			Refresh();

			// Matches are in item order
			var match = m_matches.BinarySearch(index);

			if (match < 0) {
				return null;
			}

			m_listElement.ScrollToRow(match);
			Refresh();

			return Children.TryGetValue(child, out var mirror) && mirror.IsShown ? mirror : null;
		}

		private void Refresh()
		{
			if (m_refreshing) {
				return;
			}

			m_refreshing = true;

			try {
				if (m_rematch || m_matchedVersion != m_list.ItemsVersion) {
					Rematch();
				}

				m_listElement.GetVisibleRange(m_matches.Count, out var first, out var end);
				ShowRange(first, end);
			} finally {
				m_refreshing = false;
			}
		}

		private void Rematch()
		{
			m_rematch = false;
			m_matchedVersion = m_list.ItemsVersion;
			m_matches.Clear();
			m_query.Set(Query);

			var showAll = !m_list.IsSearchable || m_list.ShowAllWhenEmpty;
			var count = m_list.ItemCount;

			for (var i = 0; i < count; i++) {
				if (m_query.IsEmpty ? showAll : Matches(i)) {
					m_matches.Add(i);
				}
			}

			// Every match can be scrolled to, so none are left over
			if (m_list.IsSearchable) {
				m_listElement.ShowSummary(new ListSummary(m_matches.Count, 0, !m_query.IsEmpty, m_list.ShowAllWhenEmpty));
			}
		}

		/// <summary>
		/// Makes elements for the matches from first to end, drops the rest, and has the element put them in place
		/// if anything moved.
		/// </summary>
		private void ShowRange(int first, int end)
		{
			var changed = m_rowsChanged || first != m_shownFirst || end != m_shownEnd || m_matches.Count != m_shownCount;

			m_wanted.Clear();
			m_rows.Clear();

			for (var i = first; i < end; i++) {
				var row = m_list.GetRow(m_matches[i]);
				m_wanted.Add(row);

				if (!Children.TryGetValue(row, out var mirror)) {
					mirror = CreateChild(row);
					changed = true;
				}

				m_rows.Add(mirror.Element);
			}

			if (Children.Count > m_wanted.Count) {
				m_stale.Clear();

				foreach (var node in Children.Keys) {
					if (!m_wanted.Contains(node)) {
						m_stale.Add(node);
					}
				}

				foreach (var node in m_stale) {
					Children[node].Dispose(true);
					Children.Remove(node);
				}

				changed = true;
			}

			if (!changed) {
				return;
			}

			m_rowsChanged = false;
			m_shownFirst = first;
			m_shownEnd = end;
			m_shownCount = m_matches.Count;
			m_listElement.ShowRows(m_matches.Count, first, m_rows);
		}

		private bool Matches(int index)
		{
			try {
				var matches = m_list.ItemMatches(index, m_query);
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
