using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Flattens a scope's elements into tab order by walking its <see cref="LeafNavigationGroup"/> tree.
	/// Entering a group at its first element and leaving it for whatever comes next both fall out of the
	/// flattened list, so tabbing is just a step through it.
	/// </summary>
	internal static class LeafTabOrderBuilder
	{
		private struct Node
		{
			public LeafNavigationElement Element;
			public LeafNavigationGroup Group;
			public Vector2Int Position;
			public int Index;
		}

		private static readonly Comparison<Node> s_leftToRight = CompareLeftToRight;
		private static readonly Comparison<Node> s_topToBottom = CompareTopToBottom;

		public static void Build(LeafNavigationScope scope, List<LeafNavigationElement> output)
		{
			output.Clear();

			var order = scope.TryGetComponent(out LeafNavigationGroup rootGroup) && rootGroup.enabled
				? rootGroup.Order
				: LeafTabOrder.Hierarchy;

			AddGroup(scope.transform, order, output);
		}

		private static void AddGroup(Transform root, LeafTabOrder order, List<LeafNavigationElement> output)
		{
			var nodes = ListPool<Node>.Get();

			CollectNodes(root, root, nodes);
			SortNodes(nodes, order);

			for (var i = 0; i < nodes.Count; i++) {
				var node = nodes[i];

				if (node.Element != null) {
					output.Add(node.Element);
					continue;
				}

				// A group can sit on an element, e.g. a selectable panel with its own controls
				if (node.Group.TryGetComponent(out LeafNavigationElement groupElement) && groupElement.enabled) {
					output.Add(groupElement);
				}

				AddGroup(node.Group.transform, node.Group.Order, output);
			}

			ListPool<Node>.Release(nodes);
		}

		private static void CollectNodes(Transform root, Transform parent, List<Node> nodes)
		{
			for (var i = 0; i < parent.childCount; i++) {
				var child = parent.GetChild(i);

				if (!child.gameObject.activeInHierarchy) {
					continue;
				}

				// A nested scope keeps its elements to itself
				if (child.TryGetComponent(out LeafNavigationScope _)) {
					continue;
				}

				if (child.TryGetComponent(out LeafNavigationGroup group) && group.enabled) {
					nodes.Add(CreateNode(root, child, null, group, nodes.Count));
					continue;
				}

				if (child.TryGetComponent(out LeafNavigationElement element) && element.enabled) {
					nodes.Add(CreateNode(root, child, element, null, nodes.Count));
				}

				CollectNodes(root, child, nodes);
			}
		}

		private static Node CreateNode(Transform root, Transform transform, LeafNavigationElement element,
			LeafNavigationGroup group, int index)
		{
			var center = transform is RectTransform rectTransform
				? (Vector3) rectTransform.rect.center
				: Vector3.zero;

			// Rounded in the group's space, so the sort is consistent and sub-unit layout jitter doesn't
			// split elements that sit on the same line
			var position = root.InverseTransformPoint(transform.TransformPoint(center));

			return new Node {
				Element = element,
				Group = group,
				Position = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y)),
				Index = index
			};
		}

		private static void SortNodes(List<Node> nodes, LeafTabOrder order)
		{
			switch (order) {
				case LeafTabOrder.LeftToRight:
					nodes.Sort(s_leftToRight);
					break;
				case LeafTabOrder.TopToBottom:
					nodes.Sort(s_topToBottom);
					break;
			}
		}

		private static int CompareLeftToRight(Node a, Node b)
		{
			var result = a.Position.x.CompareTo(b.Position.x);

			if (result == 0) {
				// Unity's y points up, higher comes first
				result = b.Position.y.CompareTo(a.Position.y);
			}

			return result != 0 ? result : a.Index.CompareTo(b.Index);
		}

		private static int CompareTopToBottom(Node a, Node b)
		{
			var result = b.Position.y.CompareTo(a.Position.y);

			if (result == 0) {
				result = a.Position.x.CompareTo(b.Position.x);
			}

			return result != 0 ? result : a.Index.CompareTo(b.Index);
		}
	}
}
