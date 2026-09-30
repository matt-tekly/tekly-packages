using System.Collections.Generic;

namespace Tekly.Trellis
{
	/// <summary>
	/// Sizes and places a line of items along one axis. Has no UnityEngine dependency.
	///
	/// Rules, in order:
	/// 1. Squeezed below the total preferred size: every item blends between its min and preferred.
	///    Below the total min, items stay at min and overflow the end.
	/// 2. Spare room: flexible items share it by weight, each capped at its max. Space a capped item
	///    can't take is shared again between the rest.
	/// 3. Space still left over is placed by the alignment: before the first item (Start, Center, End)
	///    or shared between the gaps (Space Between, Space Evenly), on top of the spacing.
	///
	/// Keep one instance per layout; it reuses scratch lists so solving doesn't allocate.
	/// </summary>
	public sealed class LineSolver
	{
		private readonly List<bool> m_capped = new List<bool>();

		private readonly List<LayoutMeasure> m_slots = new List<LayoutMeasure>();
		private readonly List<float> m_slotSizes = new List<float>();
		private readonly List<float> m_slotPositions = new List<float>();

		/// <summary>
		/// Total size of a line along its main axis, including margins and spacing.
		/// </summary>
		public static LayoutMeasure MainTotals(IReadOnlyList<LayoutMeasure> items, int start, int count, float spacing)
		{
			var min = 0f;
			var preferred = 0f;
			var flexible = 0f;

			for (var i = start; i < start + count; i++) {
				var item = items[i];
				min += item.OuterMin;
				preferred += item.OuterPreferred;
				flexible += item.Flexible;
			}

			var totalSpacing = SpacingTotal(count, spacing);

			return LayoutMeasure.Create(min + totalSpacing, preferred + totalSpacing, flexible);
		}

		/// <summary>
		/// Size of a line across its main axis: the largest item, including margins.
		/// </summary>
		public static LayoutMeasure CrossTotals(IReadOnlyList<LayoutMeasure> items, int start, int count)
		{
			var min = 0f;
			var preferred = 0f;
			var flexible = 0f;

			for (var i = start; i < start + count; i++) {
				var item = items[i];
				min = SolverMath.Max(min, item.OuterMin);
				preferred = SolverMath.Max(preferred, item.OuterPreferred);
				flexible = SolverMath.Max(flexible, item.Flexible);
			}

			return LayoutMeasure.Create(min, preferred, flexible);
		}

		public static float SpacingTotal(int count, float spacing)
		{
			return count > 1 ? spacing * (count - 1) : 0f;
		}

		/// <summary>
		/// Grow a list to at least count entries so Solve can write into it by index.
		/// </summary>
		public static void EnsureCount(List<float> list, int count)
		{
			while (list.Count < count) {
				list.Add(0f);
			}
		}

		/// <summary>
		/// Solves items [start, start + count) into a line of the given length.
		/// Writes sizes and positions at the same indices. Positions are from the start of the line
		/// to the item's own edge, after its start margin. Both lists must already hold start + count entries.
		/// </summary>
		public void Solve(IReadOnlyList<LayoutMeasure> items, int start, int count, float length, float spacing,
			LayoutAlignment alignment, List<float> sizes, List<float> positions)
		{
			var end = start + count;

			var totalMin = 0f;
			var totalPreferred = 0f;

			for (var i = start; i < end; i++) {
				totalMin += items[i].OuterMin;
				totalPreferred += items[i].OuterPreferred;
			}

			var inner = length - SpacingTotal(count, spacing);
			var minToPreferred = SolverMath.InverseLerp(totalMin, totalPreferred, inner);

			for (var i = start; i < end; i++) {
				sizes[i] = SolverMath.Lerp(items[i].Min, items[i].Preferred, minToPreferred);
			}

			var spare = SolverMath.Max(0f, inner - totalPreferred);
			spare = DistributeFlexible(items, start, end, spare, sizes);

			Distribute(spare, count, alignment, out var position, out var extraGap);

			for (var i = start; i < end; i++) {
				position += items[i].MarginStart;
				positions[i] = position;
				position += sizes[i] + items[i].MarginEnd + spacing + extraGap;
			}
		}

		/// <summary>
		/// Splits leftover space into a lead before the first item and an extra amount added to every gap.
		/// </summary>
		private static void Distribute(float spare, int count, LayoutAlignment alignment, out float lead, out float extraGap)
		{
			switch (alignment) {
				case LayoutAlignment.SpaceBetween:
					lead = 0f;
					extraGap = count > 1 ? spare / (count - 1) : 0f;
					return;
				case LayoutAlignment.SpaceEvenly:
					extraGap = spare / (count + 1);
					lead = extraGap;
					return;
				default:
					lead = spare * alignment.Factor();
					extraGap = 0f;
					return;
			}
		}

		/// <summary>
		/// Solves a line holding fewer items than a full one, sizing its items as if the line were full so
		/// they match the columns above. The missing slots borrow the measures of the reference line's items
		/// in the same columns. Then the items are placed by the alignment within the line; the spread
		/// alignments use Start so the items stay under the columns above.
		/// Falls back to Solve when the line isn't short.
		/// </summary>
		public void SolveShortLine(IReadOnlyList<LayoutMeasure> items, int start, int count, int referenceStart,
			int slots, float length, float spacing, LayoutAlignment alignment, List<float> sizes, List<float> positions)
		{
			if (count >= slots) {
				Solve(items, start, count, length, spacing, alignment, sizes, positions);
				return;
			}

			m_slots.Clear();

			for (var i = 0; i < slots; i++) {
				m_slots.Add(i < count ? items[start + i] : items[referenceStart + i]);
			}

			EnsureCount(m_slotSizes, slots);
			EnsureCount(m_slotPositions, slots);
			Solve(m_slots, 0, slots, length, spacing, LayoutAlignment.Start, m_slotSizes, m_slotPositions);

			var span = SpacingTotal(count, spacing);

			for (var i = 0; i < count; i++) {
				sizes[start + i] = m_slotSizes[i];
				span += m_slotSizes[i] + items[start + i].Margins;
			}

			var alignFactor = alignment.IsSpread() ? 0f : alignment.Factor();
			var position = SolverMath.Max(0f, length - span) * alignFactor;

			for (var i = start; i < start + count; i++) {
				position += items[i].MarginStart;
				positions[i] = position;
				position += sizes[i] + items[i].MarginEnd + spacing;
			}
		}

		/// <summary>
		/// Sizes and places one item across a line of the given length.
		/// Position is from the start of the line to the item's own edge, after its start margin.
		/// </summary>
		public static void SolveCross(LayoutMeasure item, float length, CrossAlignment alignment,
			out float size, out float position)
		{
			var space = length - item.Margins;

			if (alignment == CrossAlignment.Stretch || item.Flexible > 0f) {
				size = SolverMath.Clamp(space, item.Min, item.Max);
			} else {
				size = SolverMath.Clamp(space, item.Min, item.Preferred);
			}

			position = item.MarginStart + (space - size) * alignment.Factor();
		}

		/// <summary>
		/// Shares spare space between flexible items by weight, capping each at its max.
		/// Returns the space nobody could take.
		/// </summary>
		private float DistributeFlexible(IReadOnlyList<LayoutMeasure> items, int start, int end, float spare,
			List<float> sizes)
		{
			m_capped.Clear();

			for (var i = start; i < end; i++) {
				m_capped.Add(items[i].Flexible <= 0f || sizes[i] >= items[i].Max);
			}

			// Each round either finishes or caps at least one item, so this always ends
			for (var round = 0; round <= end - start && spare > SolverMath.EPSILON; round++) {
				var totalFlexible = 0f;

				for (var i = start; i < end; i++) {
					if (!m_capped[i - start]) {
						totalFlexible += items[i].Flexible;
					}
				}

				if (totalFlexible <= 0f) {
					break;
				}

				var perFlexible = spare / totalFlexible;
				var anyCapped = false;

				for (var i = start; i < end; i++) {
					if (m_capped[i - start]) {
						continue;
					}

					var max = items[i].Max;

					if (sizes[i] + items[i].Flexible * perFlexible >= max) {
						spare -= max - sizes[i];
						sizes[i] = max;
						m_capped[i - start] = true;
						anyCapped = true;
					}
				}

				if (anyCapped) {
					continue;
				}

				for (var i = start; i < end; i++) {
					if (!m_capped[i - start]) {
						sizes[i] += items[i].Flexible * perFlexible;
					}
				}

				spare = 0f;
			}

			return SolverMath.Max(0f, spare);
		}
	}
}
