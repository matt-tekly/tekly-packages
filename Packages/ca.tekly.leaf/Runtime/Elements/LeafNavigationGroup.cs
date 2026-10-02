using UnityEngine;

namespace Tekly.Leaf.Elements
{
	public enum LeafTabOrder
	{
		/// Sibling order, depth first.
		Hierarchy,
		/// By horizontal position, for a row whose hierarchy doesn't match what's on screen.
		LeftToRight,
		/// By vertical position, for a column whose hierarchy doesn't match what's on screen.
		TopToBottom
	}

	/// <summary>
	/// Makes the elements below it one block of the tab order. Tab reaches every element in the group before
	/// moving on, entering at its first element (or its last with Shift+Tab). Inside the group, elements and
	/// nested groups are ordered by <see cref="Order"/>, and a nested group is placed by its own rect.
	/// Put one on the <see cref="LeafNavigationScope"/> itself to set the order of the scope's top level.
	/// Only affects Tab: arrow keys still navigate spatially.
	/// </summary>
	[DisallowMultipleComponent]
	public class LeafNavigationGroup : MonoBehaviour
	{
		public LeafTabOrder Order {
			get => m_order;
			set => m_order = value;
		}

		[SerializeField] private LeafTabOrder m_order = LeafTabOrder.Hierarchy;
	}
}
