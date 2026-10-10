using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// One row of a list, built by the list's row builder for one item.
	/// </summary>
	public sealed class ListItem<T> : ScopeNode
	{
		/// <summary>
		/// The item this row shows. When the list's data hands back a different item for the same key, the row is
		/// rebuilt with the new one.
		/// </summary>
		public T Item { get; internal set; }

		private readonly Action<ListItem<T>, T> m_build;

		internal ListItem(string id, T item, Action<ListItem<T>, T> build) : base(null, null)
		{
			Item = item;
			m_build = build;
			SetRequestedId(id);
		}

		internal override string DefaultId => "item";

		protected override void RunBuilder()
		{
			m_build(this, Item);
		}
	}

	/// <summary>
	/// A row for each item of some data, kept in step with it: the data is polled while a panel shows the list, and
	/// rows are added, removed and reordered by key. Rows whose key stays keep their widgets and view state.
	///
	/// WithSearch adds a search field; each panel filters the rows by its own search text.
	/// </summary>
	public abstract class ListNode : ContainerNode
	{
		public bool IsSearchable { get; protected set; }

		/// <summary>
		/// Most rows a search shows at once. The rest are counted at the end.
		/// </summary>
		public int MaxResults { get; private set; } = 30;

		/// <summary>
		/// When set, a searchable list's results scroll in a view this tall, so the search field stays put.
		/// </summary>
		public float? ScrollHeight { get; private set; }

		/// <summary>
		/// The gap between rows.
		/// </summary>
		public int RowSpacing { get; private set; } = 4;

		public string Placeholder { get; private set; } = "Search";

		/// <summary>
		/// Whether an empty search shows the first rows (the default) or none.
		/// </summary>
		public bool ShowAllWhenEmpty { get; private set; } = true;

		protected ListNode() : base(ContainerKind.Stack)
		{
		}

		internal override string DefaultId => "list";

		public ListNode WithMaxResults(int maxResults)
		{
			EnsureEditable();
			MaxResults = Mathf.Max(1, maxResults);
			return this;
		}

		public ListNode WithScroll(float maxHeight)
		{
			EnsureEditable();
			ScrollHeight = maxHeight;
			return this;
		}

		public ListNode WithRowSpacing(int spacing)
		{
			EnsureEditable();
			RowSpacing = spacing;
			return this;
		}

		public ListNode WithPlaceholder(string placeholder)
		{
			EnsureEditable();
			Placeholder = placeholder;
			return this;
		}

		public ListNode WithShowAllWhenEmpty(bool showAll)
		{
			EnsureEditable();
			ShowAllWhenEmpty = showAll;
			return this;
		}

		/// <summary>
		/// Whether a row matches a search. words is query split on spaces; query is trimmed and never empty.
		/// </summary>
		public abstract bool Matches(Node row, string query, IReadOnlyList<string> words);
	}

	public sealed class ListNode<TItem, TKey> : ListNode
	{
		private static readonly IEqualityComparer<TItem> s_itemComparer = EqualityComparer<TItem>.Default;

		private readonly Func<IEnumerable<TItem>> m_getItems;
		private readonly Func<TItem, TKey> m_getKey;
		private readonly Action<ListItem<TItem>, TItem> m_build;
		private readonly Dictionary<TKey, ListItem<TItem>> m_rows = new();

		private Func<TItem, string> m_getText;
		private Func<TItem, string, bool> m_match;
		private Func<int> m_getVersion;

		private int m_version;
		private bool m_synced;
		private bool m_warnedDuplicate;
		private int m_lastPollFrame = -1;

		internal ListNode(Func<IEnumerable<TItem>> getItems, Func<TItem, TKey> getKey, Action<ListItem<TItem>, TItem> build)
		{
			m_getItems = getItems ?? throw new ArgumentNullException(nameof(getItems));
			m_getKey = getKey ?? throw new ArgumentNullException(nameof(getKey));
			m_build = build ?? throw new ArgumentNullException(nameof(build));
		}

		/// <summary>
		/// Adds a search field. A row matches when every typed word appears in getText of its item, ignoring case.
		/// </summary>
		public ListNode<TItem, TKey> WithSearch(Func<TItem, string> getText)
		{
			EnsureEditable();
			m_getText = getText;
			IsSearchable = true;
			return this;
		}

		/// <summary>
		/// Adds a search field with custom matching. match gets the item and the trimmed search text, which is
		/// never empty.
		/// </summary>
		public ListNode<TItem, TKey> WithSearch(Func<TItem, string, bool> match)
		{
			EnsureEditable();
			m_match = match;
			IsSearchable = true;
			return this;
		}

		/// <summary>
		/// Skips checking the data while getVersion returns the same number, for big lists that know when they change.
		/// </summary>
		public ListNode<TItem, TKey> WithVersion(Func<int> getVersion)
		{
			EnsureEditable();
			m_getVersion = getVersion;
			return this;
		}

		public override bool Matches(Node row, string query, IReadOnlyList<string> words)
		{
			if (row is not ListItem<TItem> item) {
				return false;
			}

			if (m_match != null) {
				return m_match(item.Item, query);
			}

			var text = m_getText?.Invoke(item.Item) ?? string.Empty;

			foreach (var word in words) {
				if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0) {
					return false;
				}
			}

			return true;
		}

		internal override void Freeze()
		{
			base.Freeze();

			// The first rows are built with the builder around the list, so search can find them straight away
			if (!m_synced) {
				try {
					Sync();
				} catch (Exception exception) {
					Debug.LogException(exception);
				}
			}
		}

		internal override void Poll()
		{
			// Several panels can show the same list
			if (m_lastPollFrame == Time.frameCount) {
				return;
			}

			m_lastPollFrame = Time.frameCount;

			if (m_getVersion != null && m_synced) {
				var version = m_getVersion();

				if (version == m_version) {
					return;
				}
			}

			Sync();
		}

		private void Sync()
		{
			m_synced = true;

			if (m_getVersion != null) {
				m_version = m_getVersion();
			}

			var items = m_getItems();
			var order = new List<Node>();
			var seen = new HashSet<TKey>();
			var added = new List<ListItem<TItem>>();
			var changed = new List<ListItem<TItem>>();

			BeginChanges();

			try {
				if (items != null) {
					foreach (var item in items) {
						var key = m_getKey(item);

						if (!seen.Add(key)) {
							WarnDuplicate(key);
							continue;
						}

						if (m_rows.TryGetValue(key, out var row)) {
							if (!s_itemComparer.Equals(row.Item, item)) {
								row.Item = item;
								changed.Add(row);
							}
						} else {
							row = new ListItem<TItem>(key?.ToString(), item, m_build);
							m_rows.Add(key, row);
							InsertChild(Children.Count, row);
							added.Add(row);
						}

						order.Add(row);
					}
				}

				RemoveRowsNotIn(seen);
				SetChildOrder(order);

				// New rows need their ids before they're built, so their content has paths
				AssignIds();

				foreach (var row in added) {
					row.Build();
				}

				foreach (var row in changed) {
					row.RebuildNow();
				}
			} finally {
				EndChanges();
			}
		}

		private void RemoveRowsNotIn(HashSet<TKey> keys)
		{
			if (m_rows.Count == keys.Count) {
				return;
			}

			var removed = new List<TKey>();

			foreach (var pair in m_rows) {
				if (!keys.Contains(pair.Key)) {
					removed.Add(pair.Key);
				}
			}

			foreach (var key in removed) {
				RemoveChild(m_rows[key]);
				m_rows.Remove(key);
			}
		}

		private void WarnDuplicate(TKey key)
		{
			if (!m_warnedDuplicate) {
				m_warnedDuplicate = true;
				Debug.LogWarning($"[DevBoard] A list has more than one item with the key '{key}'. Only the first is shown.");
			}
		}
	}

	public static class ListNodeFactory
	{
		/// <summary>
		/// A row for each item, built by buildRow and kept in step with getItems by getKey. getItems is polled while
		/// a panel shows the list, so it should return data that's already loaded, never fetch it.
		/// </summary>
		public static ListNode<TItem, TKey> List<TItem, TKey>(this ContainerNode parent, Func<IEnumerable<TItem>> getItems,
			Func<TItem, TKey> getKey, Action<ListItem<TItem>, TItem> buildRow)
		{
			return parent.Add(new ListNode<TItem, TKey>(getItems, getKey, buildRow));
		}

		/// <summary>
		/// A row for each item, keyed by the item itself.
		/// </summary>
		public static ListNode<T, T> List<T>(this ContainerNode parent, Func<IEnumerable<T>> getItems, Action<ListItem<T>, T> buildRow)
		{
			return parent.List(getItems, item => item, buildRow);
		}
	}
}
