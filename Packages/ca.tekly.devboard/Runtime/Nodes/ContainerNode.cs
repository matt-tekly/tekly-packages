using System;
using System.Collections.Generic;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// How a container lays out its children. Front ends pick how each looks; a container's Style overrides it.
	/// </summary>
	public enum ContainerKind
	{
		/// A plain vertical group with no background
		Stack,

		/// A vertical group with a background
		Card,

		/// A vertical list of labelled controls, with their labels lined up
		Form,

		/// Side by side
		Row
	}

	/// <summary>
	/// A node holding other nodes: a card, form, row or foldout, or a scope that builds its own content.
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
		private readonly HashSet<string> m_ids = new();

		private int m_changeDepth;
		private bool m_changed;

		public ContainerNode(ContainerKind kind = ContainerKind.Stack, string label = null) : base(label)
		{
			Kind = kind;
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
		/// Nodes inside with WithWidthGroup share widths here instead of with the rest of the nearest scope above,
		/// or at all outside a Form or tabs, which are scopes already.
		/// </summary>
		public ContainerNode WithWidthGroupScope()
		{
			EnsureEditable();
			Layout.WidthGroupScope = true;
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

		internal override string DefaultId => Kind.ToString().ToLowerInvariant();

		/// <summary>
		/// Gives every child without an id one: its requested id, label or kind as a slug, made unique among its
		/// siblings.
		/// </summary>
		internal void AssignIds()
		{
			foreach (var child in m_children) {
				if (child.Id != null) {
					continue;
				}

				var baseId = NodePaths.Slug(child.RequestedId) ?? NodePaths.Slug(child.Label) ?? child.DefaultId;
				var id = baseId;

				for (var number = 2; m_ids.Contains(id); number++) {
					id = $"{baseId}-{number}";
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
		/// Whether nodes inside share their width groups here, see <see cref="ContainerNode.WithWidthGroupScope"/>.
		/// </summary>
		public bool WidthGroupScope { get; internal set; }
	}

	public static class ContainerNodeFactory
	{
		/// <summary>
		/// A container with a background, for grouping widgets.
		/// </summary>
		public static ContainerNode Card(this ContainerNode parent, Style<ContainerNode> style = default)
		{
			return parent.Add(Container(ContainerKind.Card, style));
		}

		/// <summary>
		/// A vertical list of labelled controls, with their labels lined up.
		/// </summary>
		public static ContainerNode Form(this ContainerNode parent, Style<ContainerNode> style = default)
		{
			return parent.Add(Container(ContainerKind.Form, style));
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
		public static ContainerNode Stack(this ContainerNode parent, Style<ContainerNode> style = default)
		{
			return parent.Add(Container(ContainerKind.Stack, style));
		}

		private static ContainerNode Container(ContainerKind kind, Style<ContainerNode> style)
		{
			var container = new ContainerNode(kind);
			container.SetStyle(style.Name);

			return container;
		}
	}
}
