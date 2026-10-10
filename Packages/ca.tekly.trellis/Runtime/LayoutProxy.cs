using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Trellis
{
	/// <summary>
	/// Reports its size from a descendant (the source) plus the space around it, so parent layouts can
	/// size things whose content sits deeper, like a ScrollRect: ScrollView > Viewport > Content.
	///
	/// - The source defaults to the ScrollRect's Content on this object.
	/// - Objects between here and the source need a LayoutRelay (the inspector can add them).
	/// - Doesn't move or size its children; the Viewport and scrollbars keep their anchors.
	/// - Space around the source = the source parent's inset offsets (viewport insets, scrollbars). The parent
	///   needs to be stretched across this object (anchors 0 to 1), otherwise there's no space around.
	/// - On an axis the ScrollRect scrolls, the min size is just that space: the view can shrink and scroll.
	/// - Item settings and Fit apply as usual, so Max Height gives "grow with content, then scroll".
	/// - The source should size itself to its content (Fit Preferred) or scrolling won't work.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[AddComponentMenu("Layout/Trellis/Layout Proxy")]
	public class LayoutProxy : LayoutContainer
	{
		[Tooltip("The descendant whose size this reports. Empty uses the ScrollRect's Content")]
		[SerializeField] private RectTransform m_source;

		[Tooltip("Report the source's width. Off: width comes from the Item settings only")]
		[SerializeField] private bool m_useSourceWidth;

		[Tooltip("Report the source's height. Off: height comes from the Item settings only")]
		[SerializeField] private bool m_useSourceHeight = true;

		public RectTransform Source {
			get => m_source;
			set {
				if (m_source != value) {
					m_source = value;
					SetDirty();
				}
			}
		}

		public bool UseSourceWidth {
			get => m_useSourceWidth;
			set {
				if (m_useSourceWidth != value) {
					m_useSourceWidth = value;
					SetDirty();
				}
			}
		}

		public bool UseSourceHeight {
			get => m_useSourceHeight;
			set {
				if (m_useSourceHeight != value) {
					m_useSourceHeight = value;
					SetDirty();
				}
			}
		}

		/// <summary>
		/// The source in use: the assigned one, or the ScrollRect's Content. Null when missing or when
		/// it isn't a descendant of this object.
		/// </summary>
		public RectTransform ResolvedSource {
			get {
				var source = m_source;

				if (source == null) {
					var scrollRect = GetComponent<ScrollRect>();
					source = scrollRect != null ? scrollRect.content : null;
				}

				if (source == null || source == transform || !source.IsChildOf(transform)) {
					return null;
				}

				return source;
			}
		}

		protected override LayoutMeasure CalculateContent(int axis)
		{
			var source = ResolvedSource;
			var useSource = axis == 0 ? m_useSourceWidth : m_useSourceHeight;

			if (source == null || !useSource) {
				return LayoutMeasure.Create(0f, 0f);
			}

			var measure = MeasureSource(source, axis);
			var around = SpaceAround(source, axis);
			var min = Scrolls(axis) ? 0f : measure.Min;

			return LayoutMeasure.Create(min + around, measure.Preferred + around, measure.Flexible);
		}

		protected override void Arrange(int axis)
		{
			// Children keep their anchors; the source arranges its own content
		}

		private static LayoutMeasure MeasureSource(RectTransform source, int axis)
		{
			var item = source.GetComponent<LayoutItem>();

			if (item != null && item.isActiveAndEnabled) {
				return item.Measure(axis);
			}

			return LayoutMeasure.Create(LayoutUtility.GetMinSize(source, axis),
				LayoutUtility.GetPreferredSize(source, axis),
				LayoutUtility.GetFlexibleSize(source, axis));
		}

		/// <summary>
		/// The space between this rect and the source's parent (e.g. the Viewport), read from the parent's offsets
		/// when it's a direct child stretched across this rect. Never from our own size: that would feed back into
		/// itself (an unstretched viewport makes it grow every rebuild). A parent that isn't stretched doesn't
		/// follow our size, so there's no space around to report.
		/// </summary>
		private float SpaceAround(RectTransform source, int axis)
		{
			if (source.parent == transform || !(source.parent is RectTransform parent)) {
				return 0f;
			}

			if (parent.parent == transform && parent.anchorMin[axis] == 0f && parent.anchorMax[axis] == 1f) {
				return Mathf.Max(0f, -parent.sizeDelta[axis]);
			}

			return 0f;
		}

		private bool Scrolls(int axis)
		{
			var scrollRect = GetComponent<ScrollRect>();

			if (scrollRect == null || !scrollRect.isActiveAndEnabled) {
				return false;
			}

			return axis == 0 ? scrollRect.horizontal : scrollRect.vertical;
		}
	}
}
