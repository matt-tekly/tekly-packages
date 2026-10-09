using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Scrolling
{
	/// <summary>
	/// ScrollRect that ignores dragging and the scroll wheel while Leaf input is disabled
	/// (<see cref="LeafInputExtensions.IsLeafInputDisabled"/>). A drag in progress when input is disabled ends
	/// there, as if released, so the content doesn't jump to the pointer when input comes back.
	/// </summary>
	public class LeafScrollRect : ScrollRect
	{
		public override void OnInitializePotentialDrag(PointerEventData eventData)
		{
			// Pressing stops the content's inertia, which is input too
			if (this.IsLeafInputDisabled()) {
				return;
			}

			base.OnInitializePotentialDrag(eventData);
		}

		public override void OnBeginDrag(PointerEventData eventData)
		{
			if (this.IsLeafInputDisabled()) {
				return;
			}

			base.OnBeginDrag(eventData);
		}

		public override void OnDrag(PointerEventData eventData)
		{
			if (this.IsLeafInputDisabled()) {
				// Ends the drag, so later OnDrag calls do nothing even once input is back
				base.OnEndDrag(eventData);
				return;
			}

			base.OnDrag(eventData);
		}

		public override void OnScroll(PointerEventData data)
		{
			if (this.IsLeafInputDisabled()) {
				return;
			}

			base.OnScroll(data);
		}
	}
}
