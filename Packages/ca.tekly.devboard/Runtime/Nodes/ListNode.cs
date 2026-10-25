using System;
using System.Collections.Generic;
using Tekly.Common.Search;
using UnityEngine;
using UnityEngine.Pool;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// A list row, whatever its item type.
	/// </summary>
	public interface IListRow
	{
		/// <summary>
		/// The row's key as text, e.g. to tell apart controls with the same label in different rows.
		/// </summary>
		string KeyText { get; }
	}

	/// <summary>
	/// One row of a list, built by the list's row builder for one item.
	/// </summary>
	public sealed class ListItem<T> : ScopeNode, IListRow
	{
		/// <summary>
		/// The item this row shows. When the list's data hands back a different item for the same key, the row is
		/// rebuilt with the new one.
		/// </summary>
		public T Item { get; internal set; }

		public string KeyText { get; }

		/// <summary>
		/// Where the row's item is in the list's data, as of the last sync.
		/// </summary>
		internal int ListIndex { get; set; }

		private readonly Action<ListItem<T>, T> m_build;

		internal ListItem(string keyText, T item, Action<ListItem<T>, T> build) : base(null, null)
		{
			Item = item;
			KeyText = keyText;
			m_build = build;
			SetRequestedId(keyText);
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
	///
	/// WithVirtualRows makes the list virtual: every row is the same height, so panels only make widgets for the rows on
	/// screen and scroll through every match, and a row's nodes are only built the first time a panel shows it.
	/// </summary>
	public abstract class ListNode : ContainerNode
	{
		/// <summary>
		/// How many rows tall a virtual list's view is without WithScroll.
		/// </summary>
		public const int DEFAULT_VIRTUAL_ROWS = 10;

		public bool IsSearchable { get; protected set; }

		/// <summary>
		/// Every row's height, for a virtual list. Null for a list that builds every row and shows up to MaxResults.
		/// </summary>
		public float? RowHeight { get; private set; }

		/// <summary>
		/// Only the rows on screen get widgets, and rows are built as they're first shown. See WithVirtualRows.
		/// </summary>
		public bool IsVirtual => RowHeight != null;

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

		/// <summary>
		/// Goes up whenever the items, or their order, change. Indexes into the items are only good for one version.
		/// </summary>
		internal int ItemsVersion { get; private protected set; }

		protected ListNode() : base(ContainerKind.Column)
		{
		}

		internal override string DefaultId => "list";

		/// <summary>
		/// Always a level: each panel saves its search by its path, and its rows are ids under it.
		/// </summary>
		internal override bool AddsPathLevel => true;

		// Set through the With extensions in ListNodeSettings, which keep the caller's list type for chaining

		internal void SetMaxResults(int maxResults)
		{
			EnsureEditable();
			MaxResults = Mathf.Max(1, maxResults);
		}

		internal void SetScroll(float maxHeight)
		{
			EnsureEditable();
			ScrollHeight = maxHeight;
		}

		internal void SetRowSpacing(int spacing)
		{
			EnsureEditable();
			RowSpacing = spacing;
		}

		internal void SetPlaceholder(string placeholder)
		{
			EnsureEditable();
			Placeholder = placeholder;
		}

		internal void SetShowAllWhenEmpty(bool showAll)
		{
			EnsureEditable();
			ShowAllWhenEmpty = showAll;
		}

		internal void SetVirtualRows(float rowHeight)
		{
			EnsureEditable();
			RowHeight = Mathf.Max(1f, rowHeight);
		}

		/// <summary>
		/// Whether a row matches a search, which is never empty.
		/// </summary>
		public abstract bool Matches(Node row, SearchQuery query);

		/// <summary>
		/// How many items the data had at the last sync. A virtual list has a row for each, built or not.
		/// </summary>
		internal abstract int ItemCount { get; }

		/// <summary>
		/// Whether the item at index matches a search, which is never empty, without building its row.
		/// </summary>
		internal abstract bool ItemMatches(int index, SearchQuery query);

		/// <summary>
		/// The row for the item at index, building it first if it hasn't been.
		/// </summary>
		internal abstract Node GetRow(int index);

		/// <summary>
		/// Where row's item is in the items, or -1 if row isn't one of this list's rows.
		/// </summary>
		internal abstract int IndexOfRow(Node row);
	}

	public sealed class ListNode<TItem, TKey> : ListNode
	{
		private static readonly IEqualityComparer<TItem> s_itemComparer = EqualityComparer<TItem>.Default;

		private readonly Func<IEnumerable<TItem>> m_getItems;
		private readonly Func<TItem, TKey> m_getKey;
		private readonly Action<ListItem<TItem>, TItem> m_build;
		private readonly Dictionary<TKey, Entry> m_byKey = new();

		// The items in order, as of the last sync
		private List<Entry> m_entries = new();
		private List<Entry> m_nextEntries = new();

		private Func<TItem, string> m_getText;
		private Func<TItem, string, bool> m_match;
		private Func<int> m_getVersion;

		private int m_version;
		private int m_syncCount;
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
		/// Adds a search field, matching getText of each row's item the way the command palette matches names:
		/// every typed term must match, loosely (see <see cref="SearchQuery"/>).
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

		public override bool Matches(Node row, SearchQuery query)
		{
			if (row is not ListItem<TItem> item) {
				return false;
			}

			if (m_match != null) {
				return m_match(item.Item, query.Text);
			}

			return query.Matches(m_getText?.Invoke(item.Item));
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

		internal override int ItemCount => m_entries.Count;

		internal override bool ItemMatches(int index, SearchQuery query)
		{
			var item = m_entries[index].Item;

			if (m_match != null) {
				return m_match(item, query.Text);
			}

			return query.Matches(m_getText?.Invoke(item));
		}

		internal override Node GetRow(int index)
		{
			var entry = m_entries[index];

			if (entry.Row != null) {
				return entry.Row;
			}

			// Built rows stay in item order among the children
			var position = 0;

			for (var i = 0; i < index; i++) {
				if (m_entries[i].Row != null) {
					position++;
				}
			}

			BeginChanges();

			try {
				entry.Row = CreateRow(entry, index);
				InsertChild(position, entry.Row);

				// Its id first, so its content has paths
				AssignIds();
				entry.Row.Build();
			} finally {
				EndChanges();
			}

			return entry.Row;
		}

		internal override int IndexOfRow(Node row)
		{
			return row is ListItem<TItem> { Parent: { } parent } item && parent == this ? item.ListIndex : -1;
		}

		/// <summary>
		/// Brings the items in line with the data. A list that isn't virtual builds a row for every new item; a
		/// virtual one leaves that until a panel shows it. Built rows whose item changed are rebuilt either way.
		/// </summary>
		private void Sync()
		{
			// Read before anything is committed: a getter that throws leaves the list as it was, to be tried again
			var version = m_getVersion?.Invoke() ?? 0;
			var items = m_getItems();
			var itemsChanged = false;
			var committed = false;

			// Pooled, since a list without a version syncs every frame it's shown
			using (ListPool<ListItem<TItem>>.Get(out var added))
			using (ListPool<ListItem<TItem>>.Get(out var changed)) {
				m_syncCount++;
				m_nextEntries.Clear();

				BeginChanges();

				try {
					if (items != null) {
						foreach (var item in items) {
							var key = m_getKey(item);

							if (m_byKey.TryGetValue(key, out var entry)) {
								if (entry.LastSync == m_syncCount) {
									WarnDuplicate(key);
									continue;
								}

								if (!s_itemComparer.Equals(entry.Item, item)) {
									entry.Item = item;
									itemsChanged = true;

									if (entry.Row != null) {
										entry.Row.Item = item;
										changed.Add(entry.Row);
									}
								}
							} else {
								entry = new Entry(key, item, m_syncCount);
								m_byKey.Add(key, entry);
								itemsChanged = true;

								if (!IsVirtual) {
									entry.Row = CreateRow(entry, m_nextEntries.Count);
									InsertChild(Children.Count, entry.Row);
									added.Add(entry.Row);
								}
							}

							// Marks it seen this sync
							entry.LastSync = m_syncCount;
							m_nextEntries.Add(entry);
						}
					}

					itemsChanged |= RemoveEntriesNotSynced() || !SameOrder();

					(m_entries, m_nextEntries) = (m_nextEntries, m_entries);
					m_nextEntries.Clear();
					m_synced = true;
					m_version = version;
					committed = true;

					using (ListPool<Node>.Get(out var order)) {
						for (var i = 0; i < m_entries.Count; i++) {
							var row = m_entries[i].Row;

							if (row != null) {
								row.ListIndex = i;
								order.Add(row);
							}
						}

						SetChildOrder(order);
					}

					// Panels re-match against the new items before anything asks for a row by index
					if (itemsChanged) {
						ItemsVersion++;
					}

					// New rows need their ids before they're built, so their content has paths
					AssignIds();

					foreach (var row in added) {
						row.Build();
					}

					foreach (var row in changed) {
						row.RebuildNow();
					}
				} finally {
					if (!committed) {
						DiscardSync(changed);
					}

					EndChanges();
				}
			}
		}

		/// <summary>
		/// Undoes a sync that threw partway: the items it added go, with their rows, so the next sync starts from the
		/// last good one. Rows whose item it had already changed are rebuilt, as a finished sync would have.
		/// </summary>
		private void DiscardSync(List<ListItem<TItem>> changed)
		{
			using (ListPool<Entry>.Get(out var created)) {
				foreach (var entry in m_byKey.Values) {
					if (entry.CreatedSync == m_syncCount) {
						created.Add(entry);
					}
				}

				foreach (var entry in created) {
					if (entry.Row != null) {
						RemoveChild(entry.Row);
					}

					m_byKey.Remove(entry.Key);
				}
			}

			m_nextEntries.Clear();

			foreach (var row in changed) {
				row.RebuildNow();
			}
		}

		private ListItem<TItem> CreateRow(Entry entry, int index)
		{
			return new ListItem<TItem>(entry.Key?.ToString(), entry.Item, m_build) { ListIndex = index };
		}

		/// <summary>
		/// Drops the items this sync didn't see, and their rows. True if there were any.
		/// </summary>
		private bool RemoveEntriesNotSynced()
		{
			if (m_byKey.Count == m_nextEntries.Count) {
				return false;
			}

			using (ListPool<Entry>.Get(out var removed)) {
				foreach (var entry in m_byKey.Values) {
					if (entry.LastSync != m_syncCount) {
						removed.Add(entry);
					}
				}

				foreach (var entry in removed) {
					if (entry.Row != null) {
						RemoveChild(entry.Row);
					}

					m_byKey.Remove(entry.Key);
				}
			}

			return true;
		}

		private bool SameOrder()
		{
			if (m_entries.Count != m_nextEntries.Count) {
				return false;
			}

			for (var i = 0; i < m_entries.Count; i++) {
				if (m_entries[i] != m_nextEntries[i]) {
					return false;
				}
			}

			return true;
		}

		private void WarnDuplicate(TKey key)
		{
			if (!m_warnedDuplicate) {
				m_warnedDuplicate = true;
				Debug.LogWarning($"[DevBoard] A list has more than one item with the key '{key}'. Only the first is shown.");
			}
		}

		/// <summary>
		/// One item of the data, and its row once it's built.
		/// </summary>
		private sealed class Entry
		{
			public readonly TKey Key;
			public TItem Item;
			public ListItem<TItem> Row;

			// The last sync that saw it, to spot duplicate keys and items that have gone
			public int LastSync;

			// The sync that added it, so one that throws can take back what it added
			public readonly int CreatedSync;

			public Entry(TKey key, TItem item, int createdSync)
			{
				Key = key;
				Item = item;
				CreatedSync = createdSync;
			}
		}
	}

	/// <summary>
	/// Settings for every kind of list. Extensions rather than methods so they hand back the caller's own list type,
	/// and typed settings like WithSearch and WithVersion can still follow them.
	/// </summary>
	public static class ListNodeSettings
	{
		/// <summary>
		/// Most rows a search shows at once. The rest are counted at the end. Virtual lists show every match.
		/// </summary>
		public static T WithMaxResults<T>(this T list, int maxResults) where T : ListNode
		{
			list.SetMaxResults(maxResults);
			return list;
		}

		/// <summary>
		/// Scrolls the rows in a view at most maxHeight tall, so the search field stays put.
		/// </summary>
		public static T WithScroll<T>(this T list, float maxHeight) where T : ListNode
		{
			list.SetScroll(maxHeight);
			return list;
		}

		public static T WithRowSpacing<T>(this T list, int spacing) where T : ListNode
		{
			list.SetRowSpacing(spacing);
			return list;
		}

		public static T WithPlaceholder<T>(this T list, string placeholder) where T : ListNode
		{
			list.SetPlaceholder(placeholder);
			return list;
		}

		/// <summary>
		/// Whether an empty search shows the first rows (the default) or none.
		/// </summary>
		public static T WithShowAllWhenEmpty<T>(this T list, bool showAll) where T : ListNode
		{
			list.SetShowAllWhenEmpty(showAll);
			return list;
		}

		/// <summary>
		/// Makes the list virtual: every row is rowHeight tall, panels only make widgets for the rows on screen, and
		/// rows are built the first time a panel shows them. Searches show every match, scrolling, so MaxResults
		/// doesn't apply. The rows scroll in a view WithScroll tall, or DEFAULT_VIRTUAL_ROWS rows without it.
		/// </summary>
		public static T WithVirtualRows<T>(this T list, float rowHeight) where T : ListNode
		{
			list.SetVirtualRows(rowHeight);
			return list;
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
