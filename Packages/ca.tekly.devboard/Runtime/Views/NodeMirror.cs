using System;
using System.Collections.Generic;
using Tekly.Common.Search;
using Tekly.DevBoard.Nodes;
using UnityEngine;

namespace Tekly.DevBoard.Views
{
	/// <summary>
	/// One host's view of a node, kept in step with it: shows and hides its element as VisibleWhen says, greys it
	/// out as InteractableWhen says, and for
	/// containers creates, removes and orders the children's elements as the node's children change. Knows nothing
	/// about widgets: elements do the showing, so a panel and a remote viewer share all of this.
	///
	/// Make one for a root node with Create, call Tick as the host refreshes, and Dispose when done.
	/// </summary>
	public class NodeMirror
	{
		public Node Node { get; }
		public IViewElement Element { get; }

		/// <summary>
		/// The node is visible and its parent isn't hiding it.
		/// </summary>
		public bool IsShown => m_visible && !m_filteredOut;

		/// <summary>
		/// Hidden by its parent, e.g. a list row a search doesn't match. Separate from the node's own visibility.
		/// </summary>
		public bool FilteredOut {
			get => m_filteredOut;
			set {
				if (m_filteredOut != value) {
					m_filteredOut = value;
					ApplyShown();
				}
			}
		}

		protected ViewContext Context { get; }

		private bool m_visible = true;
		private bool m_filteredOut;
		private bool m_shown = true;
		private bool m_visibleFaulted;
		private bool m_interactable = true;
		private bool m_interactableFaulted;
		private bool m_hasAppliedInteractable;

		protected NodeMirror(Node node, IViewElement element, ViewContext context)
		{
			Node = node;
			Element = element;
			Context = context;
		}

		/// <summary>
		/// The mirror for node, shown with element. Containers get the elements for their children from element.
		/// </summary>
		public static NodeMirror Create(Node node, IViewElement element, ViewContext context)
		{
			NodeMirror mirror;

			if (node is ListNode list && element is IListElement listElement) {
				mirror = new ListMirror(list, listElement, context);
			} else if (node is TabsNode tabs && element is ITabsElement tabsElement) {
				mirror = new TabsMirror(tabs, tabsElement, context);
			} else if (node is ContainerNode container && element is IContainerElement containerElement) {
				mirror = new ContainerMirror(container, containerElement, context);
			} else {
				mirror = new NodeMirror(node, element, context);
			}

			mirror.Start();
			return mirror;
		}

		/// <summary>
		/// Called as the host refreshes, while the mirror's parent is on screen.
		/// </summary>
		public virtual void Tick()
		{
			RefreshVisible();
			RefreshInteractable();
		}

		/// <summary>
		/// Makes target show, if it's this node or inside it, and returns its mirror. Opens what's closed on the
		/// way, like foldouts. Null if it can't be shown, e.g. VisibleWhen is hiding it, it's only for the other
		/// panel mode, or it's a list row the host's search is hiding (the search is the host's to change).
		/// </summary>
		public virtual NodeMirror Reveal(Node target)
		{
			return target == Node && IsShown ? this : null;
		}

		/// <summary>
		/// Stops following the node. destroyElement is false when the element is going anyway, e.g. with its parent's.
		/// </summary>
		public virtual void Dispose(bool destroyElement)
		{
			if (destroyElement) {
				Element.Destroy();
			}
		}

		/// <summary>
		/// Runs once the mirror is made. Containers create their children's elements here.
		/// </summary>
		protected virtual void Start()
		{
			RefreshVisible();
			RefreshInteractable();
		}

		/// <summary>
		/// Logs exception the first time something starts failing, rather than every refresh while it keeps failing.
		/// </summary>
		protected static void ReportFault(ref bool faulted, Exception exception)
		{
			if (!faulted) {
				faulted = true;
				Debug.LogException(exception);
			}
		}

		private void RefreshVisible()
		{
			var visible = Node.Visible;

			if (visible == null) {
				return;
			}

			try {
				m_visible = visible();
				m_visibleFaulted = false;
			} catch (Exception exception) {
				ReportFault(ref m_visibleFaulted, exception);
				return;
			}

			ApplyShown();
		}

		/// <summary>
		/// Only this node's own check: a container's element greys out what's inside it, so children don't need to
		/// look up. A check that throws counts as not interactable.
		/// </summary>
		private void RefreshInteractable()
		{
			var interactable = Node.Interactable;

			if (interactable == null || Element is not IInteractableElement element) {
				return;
			}

			bool value;

			try {
				value = interactable();
				m_interactableFaulted = false;
			} catch (Exception exception) {
				ReportFault(ref m_interactableFaulted, exception);
				value = false;
			}

			if (value != m_interactable || !m_hasAppliedInteractable) {
				m_interactable = value;
				m_hasAppliedInteractable = true;
				element.SetInteractable(value);
			}
		}

		private void ApplyShown()
		{
			if (IsShown != m_shown) {
				m_shown = IsShown;
				Element.SetShown(m_shown);
			}
		}
	}

	/// <summary>
	/// One host's view of a container node and its children.
	/// </summary>
	public class ContainerMirror : NodeMirror
	{
		protected ContainerNode Container { get; }
		protected IContainerElement ContainerElement { get; }

		/// <summary>
		/// The mirrors of the children that have elements. Children limited to the other panel mode have none,
		/// and so may list rows a search hasn't shown yet.
		/// </summary>
		protected Dictionary<Node, NodeMirror> Children { get; } = new();

		private readonly List<IViewElement> m_order = new();
		private bool m_pollFaulted;

		internal ContainerMirror(ContainerNode node, IContainerElement element, ViewContext context) : base(node, element, context)
		{
			Container = node;
			ContainerElement = element;
		}

		public override void Tick()
		{
			base.Tick();

			if (!IsShown) {
				return;
			}

			try {
				Container.Poll();
				m_pollFaulted = false;
			} catch (Exception exception) {
				ReportFault(ref m_pollFaulted, exception);
			}

			if (!ContainerElement.ChildrenVisible) {
				return;
			}

			// By index: a child's tick can change its own children, never this list, but stay safe if it does
			var children = Container.Children;

			for (var i = 0; i < children.Count; i++) {
				if (Children.TryGetValue(children[i], out var child)) {
					child.Tick();
				}
			}
		}

		public override void Dispose(bool destroyElement)
		{
			Container.ChildrenChanged -= OnChildrenChanged;

			// Their elements go with this one's
			foreach (var child in Children.Values) {
				child.Dispose(false);
			}

			Children.Clear();
			base.Dispose(destroyElement);
		}

		public override NodeMirror Reveal(Node target)
		{
			if (target == Node || !IsShown) {
				return base.Reveal(target);
			}

			// The child of this container that target is in
			var child = target;

			while (child != null && child.Parent != Container) {
				child = child.Parent;
			}

			if (child == null) {
				return null;
			}

			ContainerElement.ShowChildren();
			return RevealChild(child)?.Reveal(target);
		}

		protected override void Start()
		{
			base.Start();

			Container.ChildrenChanged += OnChildrenChanged;
			SyncChildren();
		}

		/// <summary>
		/// The mirror of child, once it's made to show. Null if it can't be.
		/// </summary>
		protected virtual NodeMirror RevealChild(Node child)
		{
			return Children.TryGetValue(child, out var mirror) && mirror.IsShown ? mirror : null;
		}

		/// <summary>
		/// Brings the elements in line with the node's children.
		/// </summary>
		protected virtual void SyncChildren()
		{
			RemoveStaleChildren();

			foreach (var child in Container.Children) {
				if (!Children.ContainsKey(child) && child.ShowsIn(Context.IsOverlay)) {
					CreateChild(child);
				}
			}

			OrderChildren();
		}

		protected void RemoveStaleChildren()
		{
			if (Children.Count == 0) {
				return;
			}

			var current = new HashSet<Node>(Container.Children);
			List<Node> stale = null;

			foreach (var node in Children.Keys) {
				if (!current.Contains(node)) {
					stale ??= new List<Node>();
					stale.Add(node);
				}
			}

			if (stale == null) {
				return;
			}

			foreach (var node in stale) {
				Children[node].Dispose(true);
				Children.Remove(node);
			}
		}

		protected NodeMirror CreateChild(Node child)
		{
			var mirror = Create(child, ContainerElement.CreateChild(child), Context);
			Children[child] = mirror;

			return mirror;
		}

		protected void OrderChildren()
		{
			m_order.Clear();

			foreach (var child in Container.Children) {
				if (Children.TryGetValue(child, out var mirror)) {
					m_order.Add(mirror.Element);
				}
			}

			ContainerElement.Order(m_order);
		}

		private void OnChildrenChanged(ContainerNode node)
		{
			SyncChildren();
		}
	}

	/// <summary>
	/// One host's view of a list. A searchable list only creates elements for rows the host's search shows, up to
	/// the list's MaxResults; rows it hides keep their elements, just hidden. The search text is the host's own,
	/// kept in DevBoard.State.
	/// </summary>
	public sealed class ListMirror : ContainerMirror
	{
		/// <summary>
		/// The host's search text.
		/// </summary>
		public string Query { get; private set; } = string.Empty;

		private readonly ListNode m_list;
		private readonly IListElement m_listElement;
		private readonly SearchQuery m_query = new();

		private string m_stateKey;
		private bool m_matchFaulted;

		internal ListMirror(ListNode list, IListElement element, ViewContext context) : base(list, element, context)
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

			SyncChildren();
			m_listElement.ScrollToStart();
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

		protected override void SyncChildren()
		{
			if (!m_list.IsSearchable) {
				base.SyncChildren();
				return;
			}

			RemoveStaleChildren();
			Filter();
			OrderChildren();
		}


		private void Filter()
		{
			m_query.Set(Query);

			var shown = 0;
			var hidden = 0;

			foreach (var row in Container.Children) {
				if (!row.ShowsIn(Context.IsOverlay)) {
					continue;
				}

				var matches = m_query.IsEmpty ? m_list.ShowAllWhenEmpty : Matches(row);
				Children.TryGetValue(row, out var mirror);

				if (matches && shown < m_list.MaxResults) {
					mirror ??= CreateChild(row);
					mirror.FilteredOut = false;
					shown++;
				} else {
					if (matches) {
						hidden++;
					}

					if (mirror != null) {
						mirror.FilteredOut = true;
					}
				}
			}

			m_listElement.ShowSummary(new ListSummary(shown, hidden, !m_query.IsEmpty, m_list.ShowAllWhenEmpty));
		}

		private bool Matches(Node row)
		{
			try {
				var matches = m_list.Matches(row, m_query);
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
