using System;
using System.Collections.Generic;
using Tekly.DevBoard.Nodes;
using Tekly.DevBoard.Pages;
using UnityEngine;

namespace Tekly.DevBoard.Access
{
	/// <summary>
	/// Finds nodes on any page and works them as text, for things that aren't panels: the command palette, and
	/// later a terminal or remote viewer. Nodes are found by their slug path, e.g.
	/// "sample/controls/sliders/time-scale", and read, written and invoked through the adapter registered for their
	/// type. A node's type uses the adapter for itself or its nearest base type, so custom nodes can register one.
	///
	/// Looking through pages builds every segment that hasn't been built yet.
	/// </summary>
	public sealed partial class NodeAccess
	{
		private readonly PageTree m_pages;
		private readonly Dictionary<Type, NodeAdapter> m_adapters = new();

		public NodeAccess(PageTree pages)
		{
			m_pages = pages;
			RegisterDefaults();
		}

		/// <summary>
		/// Registers how TNode is read, written and invoked. kind names it, e.g. "slider", for showing to people
		/// and for remote viewers. Leave out what TNode can't do. write returns an error message, or null on success.
		/// </summary>
		public void Register<TNode>(string kind, Func<TNode, string> read = null, Func<TNode, string, string> write = null,
			Action<TNode> invoke = null) where TNode : Node
		{
			m_adapters[typeof(TNode)] = new NodeAdapter<TNode>(kind, read, write, invoke);
		}

		/// <summary>
		/// What node is, e.g. "slider". Null for nodes nothing is registered for.
		/// </summary>
		public string KindOf(Node node)
		{
			return AdapterFor(node)?.Kind;
		}

		public bool CanRead(Node node) => AdapterFor(node)?.CanRead ?? false;
		public bool CanWrite(Node node) => AdapterFor(node)?.CanWrite ?? false;
		public bool CanInvoke(Node node) => AdapterFor(node)?.CanInvoke ?? false;

		/// <summary>
		/// The node's value as text, e.g. "0.5" or "On". False if it has no value or reading threw.
		/// </summary>
		public bool TryRead(Node node, out string value)
		{
			value = null;
			var adapter = AdapterFor(node);

			if (adapter == null || !adapter.CanRead) {
				return false;
			}

			try {
				value = adapter.Read(node);
				return true;
			} catch (Exception exception) {
				Debug.LogException(exception);
				return false;
			}
		}

		/// <summary>
		/// Sets the node's value from text. Nothing is set while the node, or a container it's in, isn't
		/// interactable. error says why when it returns false.
		/// </summary>
		public bool TryWrite(Node node, string text, out string error)
		{
			var adapter = AdapterFor(node);

			if (adapter == null || !adapter.CanWrite) {
				error = $"{Describe(node)} can't be set";
				return false;
			}

			if (!node.IsInteractable()) {
				error = $"{Describe(node)} is disabled";
				return false;
			}

			try {
				error = adapter.Write(node, text);
				return error == null;
			} catch (Exception exception) {
				Debug.LogException(exception);
				error = exception.Message;
				return false;
			}
		}

		/// <summary>
		/// Does what clicking the node does: runs a button, flips a toggle. Nothing runs while the node, or a
		/// container it's in, isn't interactable. error says why when it returns false.
		/// </summary>
		public bool TryInvoke(Node node, out string error)
		{
			var adapter = AdapterFor(node);

			if (adapter == null || !adapter.CanInvoke) {
				error = $"{Describe(node)} can't be run";
				return false;
			}

			if (!node.IsInteractable()) {
				error = $"{Describe(node)} is disabled";
				return false;
			}

			try {
				adapter.Invoke(node);
				error = null;
				return true;
			} catch (Exception exception) {
				Debug.LogException(exception);
				error = exception.Message;
				return false;
			}
		}

		/// <summary>
		/// Whether an interactive panel would show node: it and every container above it are for interactive
		/// panels and visible. A VisibleWhen that throws counts as hidden.
		/// </summary>
		public static bool IsVisible(Node node, bool overlay = false)
		{
			for (; node != null; node = node.Parent) {
				if (!node.ShowsIn(overlay)) {
					return false;
				}

				try {
					if (node.Visible != null && !node.Visible()) {
						return false;
					}
				} catch (Exception) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Every page, depth first from the root.
		/// </summary>
		public IEnumerable<PageNode> AllPages()
		{
			return PagesUnder(m_pages.Root);
		}

		/// <summary>
		/// Every node on every page, depth first, containers before their children.
		/// </summary>
		public IEnumerable<Node> AllNodes()
		{
			foreach (var page in AllPages()) {
				foreach (var node in NodesOn(page)) {
					yield return node;
				}
			}
		}

		/// <summary>
		/// Every node on page, starting with each segment's root.
		/// </summary>
		public IEnumerable<Node> NodesOn(PageNode page)
		{
			// Copied: building a segment can register more segments and pages
			foreach (var segment in ToArray(page.Segments)) {
				var root = segment.Root;

				if (root == null) {
					continue;
				}

				foreach (var node in NodesUnder(root)) {
					yield return node;
				}
			}
		}

		/// <summary>
		/// The node at a slug path, or null.
		/// </summary>
		public Node Find(string path)
		{
			path = NodePaths.SlugPath(path);

			foreach (var page in AllPages()) {
				var pagePath = NodePaths.SlugPath(page.Path);

				if (pagePath.Length > 0 && !IsUnder(path, pagePath)) {
					continue;
				}

				foreach (var segment in ToArray(page.Segments)) {
					var root = segment.Root;

					if (root != null && IsUnder(path, root.Path)) {
						var found = FindUnder(root, path);

						if (found != null) {
							return found;
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// The page at a slug path, or null.
		/// </summary>
		public PageNode FindPage(string path)
		{
			path = NodePaths.SlugPath(path);

			foreach (var page in AllPages()) {
				if (NodePaths.SlugPath(page.Path) == path) {
					return page;
				}
			}

			return null;
		}

		private static Node FindUnder(Node node, string path)
		{
			if (node.Path == path) {
				return node;
			}

			if (node is not ContainerNode container || !IsUnder(path, node.Path)) {
				return null;
			}

			foreach (var child in container.Children) {
				var found = FindUnder(child, path);

				if (found != null) {
					return found;
				}
			}

			return null;
		}

		/// <summary>
		/// True if path is parent or somewhere below it.
		/// </summary>
		private static bool IsUnder(string path, string parent)
		{
			return parent != null && path.StartsWith(parent, StringComparison.Ordinal)
				&& (path.Length == parent.Length || path[parent.Length] == '/');
		}

		private static IEnumerable<PageNode> PagesUnder(PageNode page)
		{
			yield return page;

			foreach (var child in ToArray(page.Children)) {
				foreach (var descendant in PagesUnder(child)) {
					yield return descendant;
				}
			}
		}

		private static IEnumerable<Node> NodesUnder(Node node)
		{
			yield return node;

			if (node is not ContainerNode container) {
				yield break;
			}

			foreach (var child in ToArray(container.Children)) {
				foreach (var descendant in NodesUnder(child)) {
					yield return descendant;
				}
			}
		}

		private static T[] ToArray<T>(IReadOnlyList<T> list)
		{
			var array = new T[list.Count];

			for (var i = 0; i < array.Length; i++) {
				array[i] = list[i];
			}

			return array;
		}

		private NodeAdapter AdapterFor(Node node)
		{
			for (var type = node.GetType(); type != null; type = type.BaseType) {
				if (m_adapters.TryGetValue(type, out var adapter)) {
					return adapter;
				}
			}

			return null;
		}

		private string Describe(Node node)
		{
			return $"{KindOf(node) ?? node.GetType().Name} '{node.Path}'";
		}

		private abstract class NodeAdapter
		{
			public string Kind { get; }
			public abstract bool CanRead { get; }
			public abstract bool CanWrite { get; }
			public abstract bool CanInvoke { get; }

			protected NodeAdapter(string kind)
			{
				Kind = kind;
			}

			public abstract string Read(Node node);
			public abstract string Write(Node node, string text);
			public abstract void Invoke(Node node);
		}

		private sealed class NodeAdapter<TNode> : NodeAdapter where TNode : Node
		{
			private readonly Func<TNode, string> m_read;
			private readonly Func<TNode, string, string> m_write;
			private readonly Action<TNode> m_invoke;

			public NodeAdapter(string kind, Func<TNode, string> read, Func<TNode, string, string> write, Action<TNode> invoke) : base(kind)
			{
				m_read = read;
				m_write = write;
				m_invoke = invoke;
			}

			public override bool CanRead => m_read != null;
			public override bool CanWrite => m_write != null;
			public override bool CanInvoke => m_invoke != null;

			public override string Read(Node node) => m_read((TNode) node);
			public override string Write(Node node, string text) => m_write((TNode) node, text);
			public override void Invoke(Node node) => m_invoke((TNode) node);
		}
	}
}
