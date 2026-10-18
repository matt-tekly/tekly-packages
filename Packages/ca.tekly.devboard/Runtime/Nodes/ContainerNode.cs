using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// How a container lays out its children. Front ends pick how each looks; a container's Style overrides it.
	/// </summary>
	public enum ContainerKind
	{
		/// A plain vertical group with no background. Its controls line up their labels with the rest of the
		/// segment's
		Column,

		/// A group with a background, vertical unless its Layout.Axis says otherwise. Its controls line up their
		/// labels with each other, not the rest of the segment
		Card,

		/// Side by side. Its controls' labels are only as wide as they need to be, not the segment's label column;
		/// WithWidthGroupScope(false) joins the column
		Row
	}

	/// <summary>
	/// A node holding other nodes: a column, card, row or foldout, or a scope that builds its own content.
	/// Panels listen to ChildrenChanged to keep their widgets in step.
	/// </summary>
	public class ContainerNode : Node
	{
		public ContainerKind Kind { get; }

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

		// The ids taken at this container's path level, when it is one: those of every node under it that isn't
		// under another level first. Unused on a container that adds no level, see IdScope
		private readonly HashSet<string> m_ids = new();

		private int m_changeDepth;
		private bool m_changed;

		public ContainerNode(ContainerKind kind = ContainerKind.Column, string label = null) : base(label)
		{
			Kind = kind;

			// A card is visibly a box of its own, and a row's labels rarely want the width of a column of controls
			// above and below it, so both keep their width groups to themselves. A column flows with the segment
			Layout.WidthGroupScope = kind != ContainerKind.Column;
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

		public ContainerNode WithPadding(float all)
		{
			return WithPadding(Edges.All(all));
		}

		public ContainerNode WithPadding(float left, float top, float right, float bottom)
		{
			return WithPadding(new Edges(left, right, top, bottom));
		}

		public ContainerNode WithPadding(Edges padding)
		{
			EnsureEditable();
			Layout.Padding = padding;
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

		public ContainerNode WithMaxHeight(float maxHeight)
		{
			EnsureEditable();
			Layout.MaxHeight = maxHeight;
			return this;
		}

		/// <summary>
		/// Whether nodes inside with WithWidthGroup share widths here instead of with the rest of the nearest scope
		/// above: the segment, a card, a tab, or another container with a scope. Controls' labels are in a width
		/// group, so a scope also keeps them out of the segment's label column, e.g. side by side columns of
		/// properties. Cards and rows are scopes unless told otherwise; columns aren't, so a column of controls lines
		/// up with the segment. Pass false for a row whose labels should join the segment's column.
		/// </summary>
		public ContainerNode WithWidthGroupScope(bool scoped = true)
		{
			EnsureEditable();
			Layout.WidthGroupScope = scoped;
			return this;
		}

		/// <summary>
		/// Whether children can be added right now, after the container is frozen. Scopes allow it while building.
		/// </summary>
		protected virtual bool CanAddChildren => !IsFrozen;

		/// <summary>
		/// A plain container is anonymous layout unless it's labelled or given an id. Kinds with identity of their
		/// own, like foldouts and lists, say so themselves.
		/// </summary>
		internal override bool AddsPathLevel => Label != null || RequestedId != null;

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
			ReleaseIds(child);

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

			foreach (var child in children) {
				ReleaseIds(child);
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

		internal override string DefaultId => Kind.ToString().ToLowerInvariant();

		/// <summary>
		/// Gives every node at this container's path level without an id one: its requested id, label or kind as a
		/// slug, made unique at that level. Anonymous layout is walked through in tree order, so ids read top to
		/// bottom; what's inside a child that is a level of its own is that child's to assign.
		/// </summary>
		internal void AssignIds()
		{
			AssignIds(IdScope().m_ids);
		}

		private void AssignIds(HashSet<string> ids)
		{
			foreach (var child in m_children) {
				if (!child.AddsPathLevel) {
					if (child is ContainerNode layout) {
						layout.AssignIds(ids);
					}

					continue;
				}

				if (child.Id != null) {
					continue;
				}

				var baseId = NodePaths.Slug(child.RequestedId) ?? NodePaths.Slug(child.Label) ?? child.DefaultId;
				var id = baseId;

				for (var number = 2; ids.Contains(id); number++) {
					id = $"{baseId}-{number}";
				}

				child.Id = id;
				ids.Add(id);
			}
		}

		/// <summary>
		/// The container whose path level this container's children are at: the nearest one from here up that adds
		/// a level, or the root of the tree.
		/// </summary>
		private ContainerNode IdScope()
		{
			var scope = this;

			while (!scope.AddsPathLevel && scope.Parent != null) {
				scope = scope.Parent;
			}

			return scope;
		}

		/// <summary>
		/// Frees the ids a removed child took at this level: its own, and through anonymous layout, its children's.
		/// </summary>
		private void ReleaseIds(Node child)
		{
			if (child.Id != null) {
				IdScope().m_ids.Remove(child.Id);
				return;
			}

			if (child is ContainerNode { AddsPathLevel: false } layout) {
				foreach (var grandchild in layout.m_children) {
					ReleaseIds(grandchild);
				}
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
	}

	/// <summary>
	/// Layout overrides for a container. Null keeps the prefab's value.
	/// </summary>
	public class LayoutOptions
	{
		public Edges? Padding { get; internal set; }
		public int? Spacing { get; internal set; }
		public LayoutAlignment? Alignment { get; internal set; }
		public CrossAlignment? CrossAlignment { get; internal set; }
		public float? MaxHeight { get; internal set; }

		/// <summary>
		/// Which way children are laid out. Set by the factories whose kind can go either way, e.g. Card.
		/// </summary>
		public LayoutAxis? Axis { get; internal set; }

		/// <summary>
		/// Whether nodes inside share their width groups here, see <see cref="ContainerNode.WithWidthGroupScope"/>.
		/// On for cards and rows by default.
		/// </summary>
		public bool WidthGroupScope { get; internal set; }
	}

	public static class ContainerNodeFactory
	{
		/// <summary>
		/// A container with a background, for grouping widgets. Vertical, or side by side with LayoutAxis.Horizontal.
		/// </summary>
		public static ContainerNode Card(this ContainerNode parent, LayoutAxis axis = LayoutAxis.Vertical,
			Style<ContainerNode> style = default)
		{
			var card = Container(ContainerKind.Card, style);
			card.Layout.Axis = axis;

			return parent.Add(card);
		}

		/// <summary>
		/// Lays nodes out side by side.
		/// </summary>
		public static ContainerNode Row(this ContainerNode parent, Style<ContainerNode> style = default)
		{
			return parent.Add(Container(ContainerKind.Row, style));
		}

		/// <summary>
		/// A row whose buttons sit edge to edge as one bar, with only its outer corners rounded. Short for
		/// Row(Styles.SegmentedRow).
		/// </summary>
		public static ContainerNode ButtonRow(this ContainerNode parent)
		{
			return parent.Row(Styles.SegmentedRow);
		}

		/// <summary>
		/// A plain vertical group, with no background.
		/// </summary>
		public static ContainerNode Column(this ContainerNode parent, Style<ContainerNode> style = default)
		{
			return parent.Add(Container(ContainerKind.Column, style));
		}

		private static ContainerNode Container(ContainerKind kind, Style<ContainerNode> style)
		{
			var container = new ContainerNode(kind);
			container.SetStyle(style.Name);

			return container;
		}
	}
}
