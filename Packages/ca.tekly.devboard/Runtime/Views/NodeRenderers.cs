using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Nodes;
using UnityEngine;

namespace Tekly.DevBoard.Views
{
	/// <summary>
	/// How each kind of node becomes a widget: which prefab to create, and how to set the widget up from the node.
	/// A node uses the registration for its own type, or failing that its nearest base type, so registering a
	/// built-in node type again replaces how it renders.
	///
	/// A node's Prefab, when set, is created instead of the registration's prefab.
	/// </summary>
	public sealed partial class NodeRenderers
	{
		private const string ERROR_COLOR = "#E5534B";

		private readonly Dictionary<Type, Entry> m_entries = new();
		private readonly HashSet<Type> m_warned = new();

		/// <summary>
		/// Renders TNode with prefab, set up by bind.
		/// </summary>
		public void Register<TNode, TWidget>(WidgetPrefab<TWidget> prefab, Action<TNode, TWidget, ViewContext> bind)
			where TNode : Node where TWidget : Widget
		{
			Register<TNode, TWidget>((node, parent) => prefab, bind);
		}

		/// <summary>
		/// Renders TNode with a prefab picked per node, e.g. depending on the parent's layout.
		/// </summary>
		public void Register<TNode, TWidget>(Func<TNode, ContainerWidget, WidgetPrefab<TWidget>> prefab,
			Action<TNode, TWidget, ViewContext> bind) where TNode : Node where TWidget : Widget
		{
			RegisterFactory<TNode, TWidget>((node, parent) => {
				var chosen = node.Prefab != null ? new WidgetPrefab<TWidget>(node.Prefab) : prefab(node, parent);
				return parent.Create(chosen);
			}, bind);
		}

		/// <summary>
		/// Renders TNode with a widget made by create, for widgets that don't come from a prefab.
		/// </summary>
		public void RegisterFactory<TNode, TWidget>(Func<TNode, ContainerWidget, TWidget> create,
			Action<TNode, TWidget, ViewContext> bind) where TNode : Node where TWidget : Widget
		{
			m_entries[typeof(TNode)] = new Entry<TNode, TWidget>(create, bind);
		}

		/// <summary>
		/// Creates the widget for node inside parent, and a view that keeps it in step with the node.
		/// </summary>
		internal NodeView Render(Node node, ContainerWidget parent, ViewContext view)
		{
			var entry = Find(node.GetType());

			if (entry == null) {
				if (m_warned.Add(node.GetType())) {
					Debug.LogError($"[DevBoard] No renderer is registered for {node.GetType().Name}.");
				}

				return ErrorView(node, parent, view, $"No renderer for {node.GetType().Name}");
			}

			Widget widget;

			try {
				widget = entry.Create(node, parent);
			} catch (Exception exception) {
				Debug.LogException(exception);
				return ErrorView(node, parent, view, $"{exception.GetType().Name}: {exception.Message}");
			}

			try {
				entry.Bind(node, widget, view);
			} catch (Exception exception) {
				// Keep the widget: a half set up control is easier to track down than a missing one
				Debug.LogException(exception);
			}

			if (node is ContainerNode container) {
				if (widget is not ContainerWidget containerWidget) {
					Debug.LogError($"[DevBoard] {container.GetType().Name} rendered as a {widget.GetType().Name}, which can't hold children.");
					return new NodeView(node, widget);
				}

				if (container is ListNode list) {
					return new ListView(list, containerWidget, view);
				}

				return new ContainerView(container, containerWidget, containerWidget, view);
			}

			return new NodeView(node, widget);
		}

		private Entry Find(Type type)
		{
			for (; type != null; type = type.BaseType) {
				if (m_entries.TryGetValue(type, out var entry)) {
					return entry;
				}
			}

			return null;
		}

		private static NodeView ErrorView(Node node, ContainerWidget parent, ViewContext view, string message)
		{
			return new NodeView(node, parent.Label($"<color={ERROR_COLOR}>{message}</color>"));
		}

		private abstract class Entry
		{
			public abstract Widget Create(Node node, ContainerWidget parent);
			public abstract void Bind(Node node, Widget widget, ViewContext view);
		}

		private sealed class Entry<TNode, TWidget> : Entry where TNode : Node where TWidget : Widget
		{
			private readonly Func<TNode, ContainerWidget, TWidget> m_create;
			private readonly Action<TNode, TWidget, ViewContext> m_bind;

			public Entry(Func<TNode, ContainerWidget, TWidget> create, Action<TNode, TWidget, ViewContext> bind)
			{
				m_create = create;
				m_bind = bind;
			}

			public override Widget Create(Node node, ContainerWidget parent)
			{
				return m_create((TNode) node, parent);
			}

			public override void Bind(Node node, Widget widget, ViewContext view)
			{
				m_bind?.Invoke((TNode) node, (TWidget) widget, view);
			}
		}
	}
}
