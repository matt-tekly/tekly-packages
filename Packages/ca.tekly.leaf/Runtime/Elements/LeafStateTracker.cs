using Tekly.Leaf.Elements.Animators;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Tracks pointer, press and selection flags independently so they can be combined into a
	/// <see cref="LeafElementState"/>. Unity's Selectable keeps these private and collapses them into
	/// a single SelectionState, which loses Highlighted/Pressed while an element is Selected.
	/// </summary>
	public sealed class LeafStateTracker
	{
		public bool IsPointerInside { get; set; }
		public bool IsPointerDown { get; set; }
		public bool IsSelected { get; set; }

		/// <summary>
		/// Forces the Pressed mode, used for submit and delayed presses.
		/// </summary>
		public bool IsPressSimulated { get; set; }

		/// <summary>
		/// Whether the element should hold its cursor: while the pointer is over it, and while it's pressed, so
		/// dragging a scrollbar or slider keeps the cursor even when the pointer drifts off it.
		/// </summary>
		public bool HoldsCursor => IsPointerInside || IsPointerDown;

		public void Clear()
		{
			IsPointerInside = false;
			IsPointerDown = false;
			IsSelected = false;
			IsPressSimulated = false;
		}

		/// <summary>
		/// Disabled elements report Normal mode but keep their selection.
		/// </summary>
		public LeafElementState GetState(bool isInteractable, bool isOn)
		{
			var flags = LeafElementFlags.None;

			if (IsSelected) {
				flags |= LeafElementFlags.Selected;
			}

			if (isOn) {
				flags |= LeafElementFlags.On;
			}

			if (!isInteractable) {
				return new LeafElementState(LeafElementMode.Normal, flags | LeafElementFlags.Disabled);
			}

			return new LeafElementState(GetMode(), flags);
		}

		private LeafElementMode GetMode()
		{
			if (IsPointerDown || IsPressSimulated) {
				return LeafElementMode.Pressed;
			}

			if (IsPointerInside) {
				return LeafElementMode.Highlighted;
			}

			return LeafElementMode.Normal;
		}
	}
}
