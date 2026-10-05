using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Tekly.Trellis
{
	/// <summary>
	/// How a layout sizes itself along an axis when no parent layout sizes it.
	/// </summary>
	public enum FitMode
	{
		/// <summary>Keep the RectTransform's size.</summary>
		None,

		/// <summary>Size to the resolved min size.</summary>
		Min,

		/// <summary>Size to the resolved preferred size (content, with Item overrides and Max applied).</summary>
		Preferred
	}

	/// <summary>
	/// Base for Trellis layouts: an item of its parent that also arranges its own children.
	///
	/// Only children with an enabled LayoutItem (or a layout, which is one) that isn't ignoring layout
	/// take part; other children are left alone.
	///
	/// LayoutRebuilder calls, children before parents when measuring and parents before children
	/// when arranging: CalculateContent(0), Arrange(0), CalculateContent(1), Arrange(1).
	/// Widths are always settled before any height is measured.
	///
	/// Fit replaces ContentSizeFitter: the layout resizes itself (around its pivot) to its resolved
	/// min or preferred size before placing children. It only applies when no parent layout sizes it.
	/// </summary>
	public abstract class LayoutContainer : LayoutItem, ILayoutGroup
	{
		[Tooltip("Resize to fit the content's width. Ignored when a parent layout sizes this object")]
		[SerializeField] private FitMode m_fitWidth = FitMode.None;

		[Tooltip("Resize to fit the content's height. Ignored when a parent layout sizes this object")]
		[SerializeField] private FitMode m_fitHeight = FitMode.None;

		private readonly List<RectTransform> m_children = new List<RectTransform>();
		private readonly List<LayoutItem> m_items = new List<LayoutItem>();

		private DrivenRectTransformTracker m_tracker;

		private LayoutMeasure m_contentHorizontal;
		private LayoutMeasure m_contentVertical;

		// Set while this layout resizes itself, so the resulting dimension change doesn't re-dirty it
		private bool m_resizingSelf;

		public FitMode FitWidth {
			get => m_fitWidth;
			set {
				if (SetPropertyUtility.SetStruct(ref m_fitWidth, value)) {
					SetDirty();
				}
			}
		}

		public FitMode FitHeight {
			get => m_fitHeight;
			set {
				if (SetPropertyUtility.SetStruct(ref m_fitHeight, value)) {
					SetDirty();
				}
			}
		}

		/// <summary>
		/// True when an enabled layout group on the parent sizes this object. Fit does nothing then.
		/// Groups that don't size their children (LayoutRelay, LayoutProxy, ScrollRect) don't count.
		/// </summary>
		public bool IsLaidOutByParent {
			get {
				var parent = transform.parent;

				if (parent == null || IgnoreLayout) {
					return false;
				}

				using (ListPool<Component>.Get(out var groups)) {
					parent.GetComponents(typeof(ILayoutGroup), groups);

					foreach (var group in groups) {
						if (group is Behaviour behaviour && behaviour.isActiveAndEnabled && SizesChildren(group)) {
							return true;
						}
					}
				}

				return false;
			}
		}

		/// <summary>
		/// False for layout groups that only exist to connect rebuilds or lay out their own parts.
		/// </summary>
		public static bool SizesChildren(Component group)
		{
			return !(group is LayoutRelay || group is LayoutProxy || group is ScrollRect);
		}

		protected int ChildCount => m_children.Count;

		/// <summary>
		/// Lay children out in reverse sibling order.
		/// </summary>
		protected virtual bool ReverseChildren => false;

		/// <summary>
		/// Size of this layout from its children along an axis, excluding this item's own overrides.
		/// Children are already measured when this is called.
		/// </summary>
		protected abstract LayoutMeasure CalculateContent(int axis);

		/// <summary>
		/// Size and place children along an axis. This object's own size on that axis is final.
		/// </summary>
		protected abstract void Arrange(int axis);

		/// <summary>
		/// A child taking part in layout, in placement order (Reverse already applied).
		/// </summary>
		protected RectTransform GetChildRect(int index)
		{
			return m_children[index];
		}

		protected LayoutMeasure MeasureChild(int index, int axis)
		{
			return m_items[index].Measure(axis);
		}

		/// <summary>
		/// Place a child along an axis. Inset is from this rect's left or top edge.
		/// </summary>
		protected void PlaceChild(int index, int axis, float inset, float size)
		{
			var child = m_children[index];

			var driven = axis == 0
				? DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX
				: DrivenTransformProperties.AnchoredPositionY | DrivenTransformProperties.SizeDeltaY;

			m_tracker.Add(this, child, DrivenTransformProperties.Anchors | driven);

			var edge = axis == 0 ? RectTransform.Edge.Left : RectTransform.Edge.Top;
			child.SetInsetAndSizeFromParentEdge(edge, inset, size);
		}

		protected sealed override LayoutMeasure MeasureContent(int axis)
		{
			return axis == 0 ? m_contentHorizontal : m_contentVertical;
		}

		protected sealed override void OnCalculateLayout(int axis)
		{
			using var marker = MeasurePass.CalculateMarker.Auto();

			// The horizontal measuring pass is always the first call of a rebuild
			if (axis == 0) {
				m_tracker.Clear();
				GatherChildren();
			}

			var content = CalculateContent(axis);

			if (axis == 0) {
				m_contentHorizontal = content;
			} else {
				m_contentVertical = content;
			}
		}

		protected override void OnDisable()
		{
			m_tracker.Clear();
			base.OnDisable();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			base.OnRectTransformDimensionsChange();

			// Our own Fit resize is part of the current rebuild
			if (m_resizingSelf) {
				return;
			}

			// A parent layout that sizes us rebuilds us as part of its own rebuild
			if (!IsLaidOutByParent) {
				SetDirty();
			}
		}

		protected virtual void OnTransformChildrenChanged()
		{
			SetDirty();
		}

		private void GatherChildren()
		{
			m_children.Clear();
			m_items.Clear();

			var self = OwnRect;

			for (var i = 0; i < self.childCount; i++) {
				var rect = self.GetChild(i) as RectTransform;

				if (rect == null) {
					continue;
				}

				var item = rect.GetComponent<LayoutItem>();

				if (item == null || !item.isActiveAndEnabled || item.IgnoreLayout) {
					continue;
				}

				m_children.Add(rect);
				m_items.Add(item);
			}

			if (ReverseChildren) {
				m_children.Reverse();
				m_items.Reverse();
			}
		}

		/// <summary>
		/// Resize to the resolved size before children are placed. The content size for this axis was
		/// calculated in the measuring pass just before, and the RectTransform updates immediately,
		/// so Arrange sees the new size.
		/// </summary>
		private void ApplyFit(int axis)
		{
			var mode = axis == 0 ? m_fitWidth : m_fitHeight;

			if (mode == FitMode.None || IsLaidOutByParent) {
				return;
			}

			var measure = Measure(axis);
			var size = mode == FitMode.Min ? measure.Min : measure.Preferred;

			m_tracker.Add(this, OwnRect, axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY);

			m_resizingSelf = true;

			try {
				OwnRect.SetSizeWithCurrentAnchors((RectTransform.Axis) axis, size);
			} finally {
				m_resizingSelf = false;
			}
		}

		void ILayoutController.SetLayoutHorizontal()
		{
			using var marker = MeasurePass.ArrangeMarker.Auto();

			ApplyFit(0);
			Arrange(0);
		}

		void ILayoutController.SetLayoutVertical()
		{
			using var marker = MeasurePass.ArrangeMarker.Auto();

			ApplyFit(1);
			Arrange(1);
		}
	}
}
