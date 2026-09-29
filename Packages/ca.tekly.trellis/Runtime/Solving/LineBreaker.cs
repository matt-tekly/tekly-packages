using System.Collections.Generic;

namespace Tekly.Trellis
{
	/// <summary>
	/// A run of consecutive items laid out on one line.
	/// </summary>
	public struct LayoutLine
	{
		public int Start;
		public int Count;

		public LayoutLine(int start, int count)
		{
			Start = start;
			Count = count;
		}

		public override string ToString()
		{
			return $"[{Start}..{Start + Count})";
		}
	}

	/// <summary>
	/// Splits items into lines for wrapping. Has no UnityEngine dependency.
	/// </summary>
	public static class LineBreaker
	{
		/// <summary>
		/// Starts a new line when the next item's preferred size plus margins and spacing won't fit,
		/// or when the line already holds maxPerLine items (0 = no limit).
		/// An item too big for any line gets a line to itself (and is squeezed by the solver).
		/// </summary>
		public static void Break(IReadOnlyList<LayoutMeasure> items, float length, float spacing, List<LayoutLine> lines,
			int maxPerLine = 0)
		{
			lines.Clear();

			var start = 0;
			var used = 0f;

			for (var i = 0; i < items.Count; i++) {
				var outer = items[i].OuterPreferred;
				var count = i - start;

				var full = maxPerLine > 0 && count >= maxPerLine;
				var overflows = used + spacing + outer > length + SolverMath.EPSILON;

				if (count > 0 && (full || overflows)) {
					lines.Add(new LayoutLine(start, count));
					start = i;
					used = outer;
				} else {
					used += (count > 0 ? spacing : 0f) + outer;
				}
			}

			if (items.Count > start) {
				lines.Add(new LayoutLine(start, items.Count - start));
			}
		}

		/// <summary>
		/// Everything on one line (wrapping off). Produces no lines when there are no items.
		/// </summary>
		public static void Single(int count, List<LayoutLine> lines)
		{
			lines.Clear();

			if (count > 0) {
				lines.Add(new LayoutLine(0, count));
			}
		}
	}
}
