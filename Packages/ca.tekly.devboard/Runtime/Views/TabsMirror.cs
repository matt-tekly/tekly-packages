using System.Collections.Generic;
using Tekly.DevBoard.Nodes;

namespace Tekly.DevBoard.Views
{
	/// <summary>
	/// One host's view of a tabs node. Only the selected tab is shown, and a tab's element is only created the first
	/// time it's selected, so tabs nobody opens cost nothing. Tabs that were shown keep their elements, hidden. The
	/// selected tab is the host's own, kept in DevBoard.State by the tab's id so it survives tabs being reordered.
	/// </summary>
	public sealed class TabsMirror : ContainerMirror
	{
		private readonly TabsNode m_tabs;
		private readonly ITabsElement m_tabsElement;

		// The tabs with buttons, in order: the TabNode children that show in this host's mode
		private readonly List<TabNode> m_shownTabs = new();
		private readonly List<string> m_labels = new();

		private string m_stateKey;
		private string m_selectedId;

		internal TabsMirror(TabsNode tabs, ITabsElement element, ViewContext context) : base(tabs, element, context)
		{
			m_tabs = tabs;
			m_tabsElement = element;
		}

		/// <summary>
		/// Shows the tab at index, in the order of the tab buttons. Remembered for the host.
		/// </summary>
		public void Select(int index)
		{
			if (index < 0 || index >= m_shownTabs.Count) {
				return;
			}

			var id = m_shownTabs[index].Id;

			if (id == m_selectedId) {
				return;
			}

			m_selectedId = id;
			DevBoard.Instance.State.Set(m_stateKey, id);

			SyncChildren();
		}

		protected override void Start()
		{
			m_stateKey = Context.StateKey(m_tabs, "tab");

			if (DevBoard.Instance.State.TryGet(m_stateKey, out string saved)) {
				m_selectedId = saved;
			}

			base.Start();
		}

		/// <summary>
		/// Revealing something in another tab selects that tab first.
		/// </summary>
		protected override NodeMirror RevealChild(Node child)
		{
			var index = m_shownTabs.IndexOf(child as TabNode);

			if (index >= 0) {
				Select(index);
			}

			return base.RevealChild(child);
		}

		protected override void SyncChildren()
		{
			RemoveStaleChildren();
			SyncButtons();

			var selected = FindSelected();

			foreach (var tab in m_shownTabs) {
				Children.TryGetValue(tab, out var mirror);

				if (tab == selected) {
					mirror ??= CreateChild(tab);
					mirror.FilteredOut = false;
				} else if (mirror != null) {
					mirror.FilteredOut = true;
				}
			}

			OrderChildren();
			m_tabsElement.ShowSelected(m_shownTabs.IndexOf(selected));
		}

		/// <summary>
		/// Rebuilds the buttons when the tabs or their labels change.
		/// </summary>
		private void SyncButtons()
		{
			var changed = false;
			var count = 0;

			foreach (var child in Container.Children) {
				if (child is not TabNode tab || !tab.ShowsIn(Context.IsOverlay)) {
					continue;
				}

				if (count >= m_shownTabs.Count || m_shownTabs[count] != tab || m_labels[count] != tab.Label) {
					changed = true;
				}

				count++;
			}

			if (!changed && count == m_shownTabs.Count) {
				return;
			}

			m_shownTabs.Clear();
			m_labels.Clear();

			foreach (var child in Container.Children) {
				if (child is TabNode tab && tab.ShowsIn(Context.IsOverlay)) {
					m_shownTabs.Add(tab);
					m_labels.Add(tab.Label);
				}
			}

			m_tabsElement.ShowTabs(m_labels, Select);
		}

		/// <summary>
		/// The saved tab, then the node's StartTab, then the first tab. Null when there are no tabs.
		/// </summary>
		private TabNode FindSelected()
		{
			if (m_shownTabs.Count == 0) {
				return null;
			}

			foreach (var tab in m_shownTabs) {
				if (tab.Id == m_selectedId) {
					return tab;
				}
			}

			if (m_tabs.StartTab != null) {
				foreach (var tab in m_shownTabs) {
					if (tab.Label == m_tabs.StartTab) {
						return tab;
					}
				}
			}

			return m_shownTabs[0];
		}
	}
}
