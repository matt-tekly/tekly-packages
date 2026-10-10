using System;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// Which panel modes a node shows in.
	/// </summary>
	public enum OverlayMode
	{
		Both,
		InteractiveOnly,
		OverlayOnly
	}

	/// <summary>
	/// One control or group of controls on a page. Nodes are plain objects with no GameObjects: a segment's builder
	/// makes them once, and every panel showing the page renders them into widgets of its own.
	///
	/// A node's settings can only be changed while the builder that made it is running. Anything that changes
	/// later goes through a binding (for values) or VisibleWhen (for whether it's shown).
	/// </summary>
	public abstract class Node
	{
		/// <summary>
		/// The text the node is labelled with. Null for none.
		/// </summary>
		public string Label { get; }

		public ContainerNode Parent { get; internal set; }

		/// <summary>
		/// Unique among the node's siblings. Assigned once its builder finishes: the id set with WithId, or the
		/// label, with "#2", "#3" and so on added when siblings would otherwise share it.
		/// </summary>
		public string Id { get; internal set; }

		/// <summary>
		/// Where the node is: the page path, the segment, then the id of each node down to this one, e.g.
		/// "Game/Economy/Cheats/Give Gold". Saved view state like foldouts is keyed by it.
		/// </summary>
		public virtual string Path {
			get {
				if (m_path != null) {
					return m_path;
				}

				// Null until this node and every one above it has its id: a nested scope's children are frozen
				// before the builder around the scope finishes and gives the scope its id
				if (Id == null) {
					return null;
				}

				string path;

				if (Parent == null) {
					path = Id;
				} else {
					var parentPath = Parent.Path;

					if (parentPath == null) {
						return null;
					}

					path = $"{parentPath}/{Id}";
				}

				if (IsFrozen) {
					m_path = path;
				}

				return path;
			}
		}

		/// <summary>
		/// The registered prefab to render with, instead of the usual one for this kind of node. Null for the usual.
		/// </summary>
		public string Prefab { get; private set; }

		/// <summary>
		/// Polled by panels showing the node. Null means always visible.
		/// </summary>
		public Func<bool> Visible { get; private set; }

		public OverlayMode Overlay { get; private set; }

		/// <summary>
		/// Extra words search matches this node by, besides its label.
		/// </summary>
		public string[] Keywords { get; private set; }

		/// <summary>
		/// True once the builder that made the node has finished. Its settings can't change after that.
		/// </summary>
		public bool IsFrozen { get; private set; }

		internal string RequestedId { get; private set; }

		private string m_path;

		protected Node(string label)
		{
			Label = label;
		}

		/// <summary>
		/// Whether panels in this mode show the node.
		/// </summary>
		public bool ShowsIn(bool overlay)
		{
			switch (Overlay) {
				case OverlayMode.InteractiveOnly:
					return !overlay;
				case OverlayMode.OverlayOnly:
					return overlay;
				default:
					return true;
			}
		}

		/// <summary>
		/// Throws if the node's builder has finished. Call it from every setting a node type adds.
		/// </summary>
		protected void EnsureEditable()
		{
			if (IsFrozen) {
				throw new InvalidOperationException(
					$"[DevBoard] {GetType().Name} '{Label}' can't be changed after its builder has finished. " +
					"Use a binding for values that change, or VisibleWhen to show and hide it.");
			}
		}

		internal void SetPrefab(string prefab)
		{
			EnsureEditable();
			Prefab = prefab;
		}

		internal void SetVisible(Func<bool> visible)
		{
			EnsureEditable();
			Visible = visible;
		}

		internal void SetOverlay(OverlayMode overlay)
		{
			EnsureEditable();
			Overlay = overlay;
		}

		internal void SetKeywords(string[] keywords)
		{
			EnsureEditable();
			Keywords = keywords;
		}

		internal void SetRequestedId(string id)
		{
			EnsureEditable();
			RequestedId = id;
		}

		/// <summary>
		/// Locks the node's settings. Containers also assign their children's ids and freeze them.
		/// </summary>
		internal virtual void Freeze()
		{
			IsFrozen = true;
		}

		/// <summary>
		/// The node is leaving the tree: run any OnDispose callbacks in it.
		/// </summary>
		internal virtual void DisposeTree()
		{
		}
	}

	/// <summary>
	/// Settings every node has. Call them from the builder that makes the node.
	/// </summary>
	public static class NodeExtensions
	{
		/// <summary>
		/// Shows the node only while visible returns true. Panels poll it as they refresh.
		/// </summary>
		public static T VisibleWhen<T>(this T node, Func<bool> visible) where T : Node
		{
			node.SetVisible(visible);
			return node;
		}

		/// <summary>
		/// Shows the node only in interactive panels, e.g. a button that would be untouchable in an overlay.
		/// </summary>
		public static T InteractiveOnly<T>(this T node) where T : Node
		{
			node.SetOverlay(OverlayMode.InteractiveOnly);
			return node;
		}

		/// <summary>
		/// Shows the node only in overlay panels, e.g. a compact readout.
		/// </summary>
		public static T OverlayOnly<T>(this T node) where T : Node
		{
			node.SetOverlay(OverlayMode.OverlayOnly);
			return node;
		}

		/// <summary>
		/// Identifies the node among its siblings instead of its label. Use it when the label changes between
		/// builds, or two siblings share one and should keep separate state.
		/// </summary>
		public static T WithId<T>(this T node, string id) where T : Node
		{
			node.SetRequestedId(id);
			return node;
		}

		/// <summary>
		/// Extra words search finds the node by.
		/// </summary>
		public static T WithKeywords<T>(this T node, params string[] keywords) where T : Node
		{
			node.SetKeywords(keywords);
			return node;
		}
	}
}
