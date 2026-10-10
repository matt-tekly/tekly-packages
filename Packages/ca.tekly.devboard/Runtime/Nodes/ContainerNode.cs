using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// A node holding other nodes: a card, form, row or foldout, or a scope that builds its own content.
	/// Panels listen to ChildrenChanged to keep their widgets in step.
	/// </summary>
	public class ContainerNode : Node
	{
		public IReadOnlyList<Node> Children => m_children;

		/// <summary>
		/// Overrides for the layout of the container's prefab. Unset values keep the prefab's.
		/// </summary>
		public LayoutOptions Layout { get; } = new();

		/// <summary>
		/// Raised after children are added, removed or reordered. Changes made together raise it once.
		/// </summary>
		public event Action<ContainerNode> ChildrenChanged;

		private readonly List<Node> m_children = new();
		private readonly HashSet<string> m_ids = new();

		private int m_changeDepth;
		private bool m_changed;

		public ContainerNode(string label = null) : base(label)
		{
		}

		/// <summary>
		/// Adds a node, for factories. Throws once the builder that made this container has finished.
		/// </summary>
		public T Add<T>(T child) where T : Node
		{
			if (child == null) {
				throw new ArgumentNullException(nameof(child));
			}

			if (child.Parent != null) {
				throw new InvalidOperationException($"[DevBoard] {child.GetType().Name} '{child.Label}' is already in a container.");
			}

			if (!CanAddChildren) {
				EnsureEditable();
			}

			InsertChild(m_children.Count, child);
			return child;
		}

		public ContainerNode WithPadding(int all)
		{
			return WithPadding(all, all, all, all);
		}

		public ContainerNode WithPadding(int left, int top, int right, int bottom)
		{
			EnsureEditable();
			Layout.Padding = (left, top, right, bottom);
			return this;
		}

		public ContainerNode WithSpacing(int spacing)
		{
			EnsureEditable();
			Layout.Spacing = spacing;
			return this;
		}

		public ContainerNode WithAlignment(LayoutAlignment alignment)
		{
			EnsureEditable();
			Layout.Alignment = alignment;
			return this;
		}

		public ContainerNode WithCrossAlignment(CrossAlignment crossAlignment)
		{
			EnsureEditable();
			Layout.CrossAlignment = crossAlignment;
			return this;
		}

		/// <summary>
		/// How much of a parent layout's spare width this container takes. 1 fills it.
		/// </summary>
		public ContainerNode WithFlexibleWidth(float flexible = 1f)
		{
			EnsureEditable();
			Layout.FlexibleWidth = flexible;
			return this;
		}

		/// <summary>
		/// How much of a parent layout's spare height this container takes. 1 fills it.
		/// </summary>
		public ContainerNode WithFlexibleHeight(float flexible = 1f)
		{
			EnsureEditable();
			Layout.FlexibleHeight = flexible;
			return this;
		}

		public ContainerNode WithMaxHeight(float maxHeight)
		{
			EnsureEditable();
			Layout.MaxHeight = maxHeight;
			return this;
		}

		/// <summary>
		/// Whether children can be added right now, after the container is frozen. Scopes allow it while building.
		/// </summary>
		protected virtual bool CanAddChildren => !IsFrozen;

		/// <summary>
		/// Called by the panels showing this container as they refresh, at most once a frame. Lists and dynamic
		/// scopes check their data here.
		/// </summary>
		internal virtual void Poll()
		{
		}

		internal override void Freeze()
		{
			base.Freeze();
			AssignIds();

			foreach (var child in m_children) {
				child.Freeze();
			}
		}

		internal override void DisposeTree()
		{
			foreach (var child in m_children) {
				child.DisposeTree();
			}
		}

		/// <summary>
		/// Changes made between BeginChanges and EndChanges raise ChildrenChanged once, at the end.
		/// </summary>
		internal void BeginChanges()
		{
			m_changeDepth++;
		}

		internal void EndChanges()
		{
			m_changeDepth--;

			if (m_changeDepth == 0 && m_changed) {
				m_changed = false;
				RaiseChildrenChanged();
			}
		}

		internal void InsertChild(int index, Node child)
		{
			child.Parent = this;
			m_children.Insert(index, child);
			MarkChanged();
		}

		/// <summary>
		/// Removes a child and runs the OnDispose callbacks inside it.
		/// </summary>
		internal void RemoveChild(Node child)
		{
			var index = m_children.IndexOf(child);

			if (index < 0) {
				return;
			}

			m_children.RemoveAt(index);

			if (child.Id != null) {
				m_ids.Remove(child.Id);
			}

			child.DisposeTree();
			MarkChanged();
		}

		/// <summary>
		/// Removes every child, running the OnDispose callbacks inside them.
		/// </summary>
		internal void ClearChildren()
		{
			if (m_children.Count == 0) {
				return;
			}

			var children = m_children.ToArray();
			m_children.Clear();
			m_ids.Clear();

			foreach (var child in children) {
				child.DisposeTree();
			}

			MarkChanged();
		}

		/// <summary>
		/// Puts the children in the order given. order must hold exactly the current children.
		/// </summary>
		internal void SetChildOrder(List<Node> order)
		{
			var same = true;

			for (var i = 0; i < order.Count; i++) {
				if (m_children[i] != order[i]) {
					same = false;
					break;
				}
			}

			if (same) {
				return;
			}

			m_children.Clear();
			m_children.AddRange(order);
			MarkChanged();
		}

		/// <summary>
		/// Gives every child without an id one: its requested id or label, made unique among its siblings.
		/// </summary>
		internal void AssignIds()
		{
			foreach (var child in m_children) {
				if (child.Id != null) {
					continue;
				}

				var baseId = Sanitize(child.RequestedId ?? child.Label ?? child.GetType().Name);
				var id = baseId;

				for (var number = 2; m_ids.Contains(id); number++) {
					id = $"{baseId}#{number}";
				}

				child.Id = id;
				m_ids.Add(id);
			}
		}

		private void MarkChanged()
		{
			if (m_changeDepth > 0) {
				m_changed = true;
			} else {
				RaiseChildrenChanged();
			}
		}

		private void RaiseChildrenChanged()
		{
			var handlers = ChildrenChanged;

			if (handlers == null) {
				return;
			}

			// One panel throwing shouldn't stop the others hearing about the change
			foreach (var handler in handlers.GetInvocationList()) {
				try {
					((Action<ContainerNode>) handler)(this);
				} catch (Exception exception) {
					Debug.LogException(exception);
				}
			}
		}

		/// <summary>
		/// Slashes separate path parts, so they can't appear in an id.
		/// </summary>
		private static string Sanitize(string id)
		{
			return id.Replace('/', '_');
		}
	}

	/// <summary>
	/// Layout overrides for a container. Null keeps the prefab's value.
	/// </summary>
	public class LayoutOptions
	{
		public (int Left, int Top, int Right, int Bottom)? Padding { get; internal set; }
		public int? Spacing { get; internal set; }
		public LayoutAlignment? Alignment { get; internal set; }
		public CrossAlignment? CrossAlignment { get; internal set; }
		public float? FlexibleWidth { get; internal set; }
		public float? FlexibleHeight { get; internal set; }
		public float? MaxHeight { get; internal set; }
	}

	public static class ContainerNodeFactory
	{
		/// <summary>
		/// A container with a background, for grouping widgets.
		/// </summary>
		public static ContainerNode Card(this ContainerNode parent, WidgetPrefab<ContainerWidget> prefab = default)
		{
			return parent.Add(Container(prefab.Or(WidgetPrefabs.Card)));
		}

		/// <summary>
		/// A vertical list of labelled controls, with their labels lined up.
		/// </summary>
		public static ContainerNode Form(this ContainerNode parent, WidgetPrefab<ContainerWidget> prefab = default)
		{
			return parent.Add(Container(prefab.Or(WidgetPrefabs.Form)));
		}

		/// <summary>
		/// Lays nodes out side by side.
		/// </summary>
		public static ContainerNode Row(this ContainerNode parent, WidgetPrefab<ContainerWidget> prefab = default)
		{
			return parent.Add(Container(prefab.Or(WidgetPrefabs.Row)));
		}

		private static ContainerNode Container(WidgetPrefab<ContainerWidget> prefab)
		{
			var container = new ContainerNode();
			container.SetPrefab(prefab.Name);

			return container;
		}
	}
}
