using System.Collections.Generic;
using Tekly.Common.Ui.Fancy;
using Tekly.Common.Ui.Styled;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Trellis
{
	/// <summary>
	/// Put next to a FlowLayout to round only the outer corners of its children's StyledRects, like a
	/// segmented button row or a stack of panels. Re-evaluated whenever the layout rebuilds, so it follows
	/// Axis, Reverse, wrapping and children being added, removed or hidden.
	///
	/// Children that still use a FancyRect are handled too, until FancyRect is removed.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(FlowLayout))]
	[AddComponentMenu("Layout/Trellis/Segment Corners")]
	public class SegmentCorners : MonoBehaviour
	{
		[Tooltip("Corners allowed to round at all, e.g. Top for tabs")]
		[SerializeField] private StyledCorners m_allowed = StyledCorners.All;

		[Tooltip("Round every corner when there is only one child")]
		[SerializeField] private bool m_roundWhenAlone = true;

		private FlowLayout m_layout;

		// The rect component on each child whose corners were set: a StyledRect, or a FancyRect.
		private HashSet<Component> m_applied = new HashSet<Component>();
		private HashSet<Component> m_previous = new HashSet<Component>();

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

					if (!TryGetRect(child, out var rect)) {
						continue;
					}

					var corners = alone && m_roundWhenAlone
						? StyledCorners.All
						: OuterCorners(horizontal, l == 0, l == lineCount - 1, i == 0, i == line.Count - 1);

					SetCorners(rect, corners & m_allowed);

					m_applied.Add(rect);
					m_previous.Remove(rect);
				}
			}

			// Children that left the layout get their corners back
			ResetAll(m_previous);
		}

		/// <summary>The component on a child whose corners this sets: its StyledRect, or failing that its FancyRect.</summary>
		private static bool TryGetRect(Component child, out Component rect)
		{
			if (child.TryGetComponent(out StyledRect styled)) {
				rect = styled;
				return true;
			}

			if (child.TryGetComponent(out FancyRect fancy)) {
				rect = fancy;
				return true;
			}

			rect = null;
			return false;
		}

		private static void SetCorners(Component rect, StyledCorners corners)
		{
			if (rect is StyledRect styled) {
				styled.RoundedCorners = corners;
			} else if (rect is FancyRect fancy) {
				// The two corner enums use the same values.
				fancy.RoundedCorners = (RectCorners)(int)corners;
			}
		}

		/// <summary>
		/// Lines are rows when horizontal, columns when vertical. With wrapping the group is one block:
		/// only the ends of the first and last lines are outer.
		/// </summary>
		private static StyledCorners OuterCorners(bool horizontal, bool firstLine, bool lastLine, bool first, bool last)
		{
			var corners = StyledCorners.None;

			if (firstLine && first) {
				corners |= StyledCorners.TopLeft;
			}

			if (lastLine && last) {
				corners |= StyledCorners.BottomRight;
			}

			if (horizontal ? firstLine && last : lastLine && first) {
				corners |= StyledCorners.TopRight;
			}

			if (horizontal ? lastLine && first : firstLine && last) {
				corners |= StyledCorners.BottomLeft;
			}

			return corners;
		}

		private static void ResetAll(HashSet<Component> rects)
		{
			foreach (var rect in rects) {
				if (rect != null) {
					SetCorners(rect, StyledCorners.All);
				}
			}

			rects.Clear();
		}
	}
}
