using System.Collections.Generic;
using Tekly.Common.Ui.Fancy;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Trellis
{
	/// <summary>
	/// Put next to a FlowLayout to round only the outer corners of its children's FancyRects, like a
	/// segmented button row or a stack of panels. Re-evaluated whenever the layout rebuilds, so it follows
	/// Axis, Reverse, wrapping and children being added, removed or hidden.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(FlowLayout))]
	[AddComponentMenu("Layout/Trellis/Segment Corners")]
	public class SegmentCorners : MonoBehaviour
	{
		[Tooltip("Corners allowed to round at all, e.g. Top for tabs")]
		[SerializeField] private RectCorners m_allowed = RectCorners.All;

		[Tooltip("Round every corner when there is only one child")]
		[SerializeField] private bool m_roundWhenAlone = true;

		private FlowLayout m_layout;

		private HashSet<FancyRect> m_applied = new HashSet<FancyRect>();
		private HashSet<FancyRect> m_previous = new HashSet<FancyRect>();

		private void OnEnable()
		{
			m_layout = GetComponent<FlowLayout>();
			m_layout.Arranged += OnArranged;
			m_layout.SetDirty();
		}

		private void OnDisable()
		{
			if (m_layout != null) {
				m_layout.Arranged -= OnArranged;
			}

			ResetAll(m_applied);
		}

		private void OnValidate()
		{
			if (isActiveAndEnabled && m_layout != null) {
				m_layout.SetDirty();
			}
		}

		private void OnArranged(FlowLayout layout)
		{
			(m_previous, m_applied) = (m_applied, m_previous);
			m_applied.Clear();

			var horizontal = layout.Axis == LayoutAxis.Horizontal;
			var lineCount = layout.LineCount;
			var alone = layout.ArrangedChildCount == 1;

			for (var l = 0; l < lineCount; l++) {
				var line = layout.GetLine(l);

				for (var i = 0; i < line.Count; i++) {
					var child = layout.GetArrangedChild(line.Start + i);

					if (!child.TryGetComponent(out FancyRect rect)) {
						continue;
					}

					var corners = alone && m_roundWhenAlone
						? RectCorners.All
						: OuterCorners(horizontal, l == 0, l == lineCount - 1, i == 0, i == line.Count - 1);

					rect.RoundedCorners = corners & m_allowed;

					m_applied.Add(rect);
					m_previous.Remove(rect);
				}
			}

			// Children that left the layout get their corners back
			ResetAll(m_previous);
		}

		/// <summary>
		/// Lines are rows when horizontal, columns when vertical. With wrapping the group is one block:
		/// only the ends of the first and last lines are outer.
		/// </summary>
		private static RectCorners OuterCorners(bool horizontal, bool firstLine, bool lastLine, bool first, bool last)
		{
			var corners = RectCorners.None;

			if (firstLine && first) {
				corners |= RectCorners.TopLeft;
			}

			if (lastLine && last) {
				corners |= RectCorners.BottomRight;
			}

			if (horizontal ? firstLine && last : lastLine && first) {
				corners |= RectCorners.TopRight;
			}

			if (horizontal ? lastLine && first : firstLine && last) {
				corners |= RectCorners.BottomLeft;
			}

			return corners;
		}

		private static void ResetAll(HashSet<FancyRect> rects)
		{
			foreach (var rect in rects) {
				if (rect != null) {
					rect.RoundedCorners = RectCorners.All;
				}
			}

			rects.Clear();
		}
	}
}
