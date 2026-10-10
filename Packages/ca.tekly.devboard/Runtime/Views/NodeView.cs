using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Nodes;
using UnityEngine;

namespace Tekly.DevBoard.Views
{
	/// <summary>
	/// One panel's widget for a node. Keeps the widget shown or hidden as the node's VisibleWhen says.
	/// </summary>
	internal class NodeView
	{
		public Node Node { get; }
		public Widget Widget { get; }

		/// <summary>
		/// Hidden by its parent, e.g. a list row a search doesn't match. Separate from the node's own visibility.
		/// </summary>
		public bool FilteredOut {
			get => m_filteredOut;
			set {
				if (m_filteredOut != value) {
					m_filteredOut = value;
					ApplyActive();
				}
			}
		}

		protected bool IsActive => Widget != null && Widget.gameObject.activeSelf;

		private bool m_visible = true;
		private bool m_filteredOut;
		private bool m_visibleFaulted;

		public NodeView(Node node, Widget widget)
		{
			Node = node;
			Widget = widget;

			RefreshVisible();
		}

		/// <summary>
		/// Called as the panel refreshes, while the view's parent is on screen.
		/// </summary>
		public virtual void Tick()
		{
			RefreshVisible();
		}

		/// <summary>
		/// Stops listening to the node. destroyWidget is false when the widget is going anyway, e.g. with its panel.
		/// </summary>
		public virtual void Dispose(bool destroyWidget)
		{
			if (destroyWidget && Widget != null) {
				ContainerWidget.DestroyWidget(Widget.gameObject);
			}
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

			bool value;

			try {
				value = visible();
				m_visibleFaulted = false;
			} catch (Exception exception) {
				ReportFault(ref m_visibleFaulted, exception);
				return;
			}

			if (value != m_visible) {
				m_visible = value;
				ApplyActive();
			}
		}

		private void ApplyActive()
		{
			var active = m_visible && !m_filteredOut;

			if (Widget != null && Widget.gameObject.activeSelf != active) {
				Widget.gameObject.SetActive(active);
			}
		}
	}

	/// <summary>
	/// One panel's widgets for a container node and its children. Follows ChildrenChanged: creates widgets for new
	/// children, destroys them for removed ones, and keeps them in the node's order.
	/// </summary>
	internal class ContainerView : NodeView
	{
		protected ContainerNode Container { get; }
		protected ViewContext View { get; }

		/// <summary>
		/// The widget children are created in. Null until set, for views that build their own structure first.
		/// </summary>
		protected ContainerWidget Target { get; private set; }

		protected readonly Dictionary<Node, NodeView> Views = new();

		// Widgets the prefab or bind put in the target before any children, e.g. a segment's heading
		private int m_firstIndex;
		private bool m_pollFaulted;

		public ContainerView(ContainerNode node, ContainerWidget widget, ContainerWidget target, ViewContext view) : base(node, widget)
		{
			Container = node;
			View = view;

			node.ChildrenChanged += OnChildrenChanged;

			if (target != null) {
				SetTarget(target);
			}
		}

		public override void Tick()
		{
			base.Tick();

			if (!IsActive) {
				return;
			}

			try {
				Container.Poll();
				m_pollFaulted = false;
			} catch (Exception exception) {
				ReportFault(ref m_pollFaulted, exception);
			}

			// Closed foldouts and the like: nothing inside is on screen
			if (Target == null || !Target.Content.gameObject.activeInHierarchy) {
				return;
			}

			var children = Container.Children;

			for (var i = 0; i < children.Count; i++) {
				if (Views.TryGetValue(children[i], out var child)) {
					child.Tick();
				}
			}
		}

		public override void Dispose(bool destroyWidget)
		{
			Container.ChildrenChanged -= OnChildrenChanged;

			// Their widgets go with this one's
			foreach (var view in Views.Values) {
				view.Dispose(false);
			}

			Views.Clear();
			base.Dispose(destroyWidget);
		}

		protected void SetTarget(ContainerWidget target)
		{
			Target = target;
			m_firstIndex = target.Content.childCount;

			SyncChildren();
		}

		/// <summary>
		/// Brings the widgets in line with the node's children.
		/// </summary>
		protected virtual void SyncChildren()
		{
			RemoveStaleViews();

			foreach (var child in Container.Children) {
				if (!Views.ContainsKey(child) && child.ShowsIn(View.IsOverlay)) {
					CreateView(child);
				}
			}

			PlaceViews();
		}

		protected void RemoveStaleViews()
		{
			if (Views.Count == 0) {
				return;
			}

			var current = new HashSet<Node>(Container.Children);
			List<Node> stale = null;

			foreach (var node in Views.Keys) {
				if (!current.Contains(node)) {
					stale ??= new List<Node>();
					stale.Add(node);
				}
			}

			if (stale == null) {
				return;
			}

			foreach (var node in stale) {
				Views[node].Dispose(true);
				Views.Remove(node);
			}
		}

		protected NodeView CreateView(Node child)
		{
			var view = DevBoard.Instance.Renderers.Render(child, Target, View);
			Views[child] = view;

			return view;
		}

		/// <summary>
		/// Orders the widgets to match the node's children, after any widgets that came before them.
		/// </summary>
		protected void PlaceViews()
		{
			var index = m_firstIndex;

			foreach (var child in Container.Children) {
				if (!Views.TryGetValue(child, out var view) || view.Widget == null) {
					continue;
				}

				var transform = view.Widget.transform;

				if (transform.GetSiblingIndex() != index) {
					transform.SetSiblingIndex(index);
				}

				index++;
			}
		}

		private void OnChildrenChanged(ContainerNode node)
		{
			if (Target != null) {
				SyncChildren();
			}
		}
	}
}
