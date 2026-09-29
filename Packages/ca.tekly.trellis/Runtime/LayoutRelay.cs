using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Trellis
{
	/// <summary>
	/// Connects a layout below to a layout above through an object that has no layout of its own,
	/// such as a ScrollRect's Viewport. Unity only rebuilds and measures through objects with layout
	/// components, so without a relay the layouts above never hear about changes below.
	///
	/// Doesn't move or size anything. Trellis layouts under a relay still size themselves with Fit.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	[AddComponentMenu("Layout/Trellis/Layout Relay")]
	public class LayoutRelay : UIBehaviour, ILayoutGroup
	{
		protected override void OnEnable()
		{
			base.OnEnable();
			MarkDirty();
		}

		protected override void OnDisable()
		{
			MarkDirty();
			base.OnDisable();
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();
			MarkDirty();
		}

		private void MarkDirty()
		{
			if (!CanvasUpdateRegistry.IsRebuildingLayout()) {
				LayoutRebuilder.MarkLayoutForRebuild((RectTransform) transform);
			}
		}

		void ILayoutController.SetLayoutHorizontal()
		{
		}

		void ILayoutController.SetLayoutVertical()
		{
		}
	}
}
