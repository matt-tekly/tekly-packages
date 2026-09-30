using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Trellis
{
	/// <summary>
	/// Lays children out along one axis, optionally wrapping onto more lines.
	///
	/// Along the axis: squeezed children blend between min and preferred, spare room goes to flexible
	/// children (up to their max), and whatever is left is placed by Alignment.
	/// Across the axis: children follow Cross Alignment within their line.
	///
	/// Wrapping works best horizontally. A vertical wrap has to decide its columns before Unity measures
	/// heights, so it uses the current height: keep that height fixed (anchors or a parent), not sized to content.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[AddComponentMenu("Layout/Trellis/Flow Layout")]
	public class FlowLayout : LayoutContainer
	{
		[SerializeField] private LayoutAxis m_axis = LayoutAxis.Vertical;
		[SerializeField] private bool m_reverse;
		[SerializeField] private Edges m_padding;
		[SerializeField] private float m_spacing;

		[Tooltip("Where children sit along the axis when there is spare room nobody flexible can take. " +
			"Space Between and Space Evenly share it between the gaps, on top of Spacing")]
		[SerializeField] private LayoutAlignment m_alignment = LayoutAlignment.Start;

		[Tooltip("How children are sized and placed across the axis, within their line")]
		[SerializeField] private CrossAlignment m_crossAlignment = CrossAlignment.Stretch;

		[SerializeField] private bool m_wrap;

		[Tooltip("Start a new line after this many children. 0 = as many as fit. " +
			"A short last line keeps the column sizes of the lines above")]
		[Min(0)]
		[SerializeField] private int m_maxPerLine;

		[SerializeField] private float m_lineSpacing;

		[Tooltip("Where lines sit across the axis when there is spare room. Stretch shares it between lines")]
		[SerializeField] private CrossAlignment m_lineAlignment = CrossAlignment.Stretch;

		private readonly LineSolver m_solver = new LineSolver();

		// Child measures per axis: [0] horizontal, [1] vertical
		private readonly List<LayoutMeasure>[] m_measures = {
			new List<LayoutMeasure>(),
			new List<LayoutMeasure>()
		};

		private readonly List<LayoutLine> m_lines = new List<LayoutLine>();
		private readonly List<LayoutMeasure> m_lineMeasures = new List<LayoutMeasure>();
		private readonly List<LayoutMeasure> m_lineSolveMeasures = new List<LayoutMeasure>();

		private readonly List<float> m_sizes = new List<float>();
		private readonly List<float> m_positions = new List<float>();
		private readonly List<float> m_lineSizes = new List<float>();
		private readonly List<float> m_linePositions = new List<float>();

		public LayoutAxis Axis {
			get => m_axis;
			set {
				if (SetPropertyUtility.SetStruct(ref m_axis, value)) {
					SetDirty();
				}
			}
		}

		public bool Reverse {
			get => m_reverse;
			set {
				if (SetPropertyUtility.SetStruct(ref m_reverse, value)) {
					SetDirty();
				}
			}
		}

		public Edges Padding {
			get => m_padding;
			set {
				if (SetPropertyUtility.SetStruct(ref m_padding, value)) {
					SetDirty();
				}
			}
		}

		public float Spacing {
			get => m_spacing;
			set {
				if (SetPropertyUtility.SetStruct(ref m_spacing, value)) {
					SetDirty();
				}
			}
		}

		public LayoutAlignment Alignment {
			get => m_alignment;
			set {
				if (SetPropertyUtility.SetStruct(ref m_alignment, value)) {
					SetDirty();
				}
			}
		}

		public CrossAlignment CrossAlignment {
			get => m_crossAlignment;
			set {
				if (SetPropertyUtility.SetStruct(ref m_crossAlignment, value)) {
					SetDirty();
				}
			}
		}

		public bool Wrap {
			get => m_wrap;
			set {
				if (SetPropertyUtility.SetStruct(ref m_wrap, value)) {
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Most children on one line when wrapping; 0 means as many as fit.
		/// </summary>
		public int MaxPerLine {
			get => m_maxPerLine;
			set {
				if (SetPropertyUtility.SetStruct(ref m_maxPerLine, Mathf.Max(0, value))) {
					SetDirty();
				}
			}
		}

		public float LineSpacing {
			get => m_lineSpacing;
			set {
				if (SetPropertyUtility.SetStruct(ref m_lineSpacing, value)) {
					SetDirty();
				}
			}
		}

		public CrossAlignment LineAlignment {
			get => m_lineAlignment;
			set {
				if (SetPropertyUtility.SetStruct(ref m_lineAlignment, value)) {
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Number of lines from the last rebuild.
		/// </summary>
		public int LineCount => m_lines.Count;

		protected override bool ReverseChildren => m_reverse;

		private int MainAxis => m_axis == LayoutAxis.Horizontal ? 0 : 1;
		private int CrossAxis => 1 - MainAxis;

		protected override LayoutMeasure CalculateContent(int axis)
		{
			FillMeasures(axis);

			// Vertical wrap: columns must be known before widths are measured, so break lines now
			// using the current height and early height measures. They are re-measured in the next pass.
			if (axis == CrossAxis && MainAxis == 1) {
				FillMeasures(MainAxis);
				BuildLines();
			}

			var padding = PaddingTotal(axis);

			if (axis == MainAxis) {
				var main = m_measures[axis];
				var totals = LineSolver.MainTotals(main, 0, main.Count, m_spacing);
				var min = m_wrap ? LargestOuterMin(main) : totals.Min;
				var preferred = UsesColumns ? WidestChunk(main) : totals.Preferred;

				return LayoutMeasure.Create(min + padding, preferred + padding, totals.Flexible);
			}

			BuildLineMeasures();

			var lines = LineSolver.MainTotals(m_lineMeasures, 0, m_lineMeasures.Count, m_lineSpacing);

			return LayoutMeasure.Create(lines.Min + padding, lines.Preferred + padding, lines.Flexible);
		}

		protected override void Arrange(int axis)
		{
			if (axis == MainAxis) {
				ArrangeMain(axis);
			} else {
				ArrangeCross(axis);
			}
		}

		private void ArrangeMain(int axis)
		{
			// Horizontal wrap: the width is final now, so this is where lines are decided
			if (MainAxis == 0) {
				BuildLines();
			}

			var main = m_measures[axis];
			var length = OwnRect.rect.size[axis] - PaddingTotal(axis);

			LineSolver.EnsureCount(m_sizes, main.Count);
			LineSolver.EnsureCount(m_positions, main.Count);

			for (var l = 0; l < m_lines.Count; l++) {
				var line = m_lines[l];

				// With a set number per line, a short last line matches the columns of the first
				if (UsesColumns && l > 0 && l == m_lines.Count - 1) {
					var reference = m_lines[0];
					m_solver.SolveShortLine(main, line.Start, line.Count, reference.Start, reference.Count, length,
						m_spacing, m_alignment, m_sizes, m_positions);
				} else {
					m_solver.Solve(main, line.Start, line.Count, length, m_spacing, m_alignment, m_sizes, m_positions);
				}
			}

			var start = PaddingStart(axis);

			for (var i = 0; i < main.Count; i++) {
				PlaceChild(i, axis, start + m_positions[i], m_sizes[i]);
			}
		}

		private void ArrangeCross(int axis)
		{
			var cross = m_measures[axis];
			var length = OwnRect.rect.size[axis] - PaddingTotal(axis);
			var start = PaddingStart(axis);

			LineSolver.EnsureCount(m_lineSizes, m_lines.Count);
			LineSolver.EnsureCount(m_linePositions, m_lines.Count);

			if (m_wrap) {
				SolveLines(length);
			} else if (m_lines.Count > 0) {
				// One line always fills the whole cross length
				m_lineSizes[0] = length;
				m_linePositions[0] = 0f;
			}

			for (var l = 0; l < m_lines.Count; l++) {
				var line = m_lines[l];

				for (var i = line.Start; i < line.Start + line.Count; i++) {
					LineSolver.SolveCross(cross[i], m_lineSizes[l], m_crossAlignment, out var size, out var position);
					PlaceChild(i, axis, start + m_linePositions[l] + position, size);
				}
			}
		}

		/// <summary>
		/// Treat each line as an item along the cross axis. Stretch gives every line an equal share of spare room.
		/// </summary>
		private void SolveLines(float length)
		{
			var flexible = m_lineAlignment == CrossAlignment.Stretch ? 1f : 0f;

			m_lineSolveMeasures.Clear();

			foreach (var line in m_lineMeasures) {
				m_lineSolveMeasures.Add(LayoutMeasure.Create(line.Min, line.Preferred, flexible));
			}

			m_solver.Solve(m_lineSolveMeasures, 0, m_lineSolveMeasures.Count, length, m_lineSpacing,
				m_lineAlignment.ToLayoutAlignment(), m_lineSizes, m_linePositions);
		}

		private void FillMeasures(int axis)
		{
			var measures = m_measures[axis];
			measures.Clear();

			for (var i = 0; i < ChildCount; i++) {
				measures.Add(MeasureChild(i, axis));
			}
		}

		private void BuildLines()
		{
			if (!m_wrap) {
				LineBreaker.Single(ChildCount, m_lines);
				return;
			}

			var length = OwnRect.rect.size[MainAxis] - PaddingTotal(MainAxis);
			LineBreaker.Break(m_measures[MainAxis], length, m_spacing, m_lines, m_maxPerLine);
		}

		private void BuildLineMeasures()
		{
			m_lineMeasures.Clear();

			var cross = m_measures[CrossAxis];

			foreach (var line in m_lines) {
				m_lineMeasures.Add(LineSolver.CrossTotals(cross, line.Start, line.Count));
			}
		}

		private bool UsesColumns => m_wrap && m_maxPerLine > 0;

		/// <summary>
		/// Preferred length of the longest run of MaxPerLine children: the size that fits N per line.
		/// </summary>
		private float WidestChunk(List<LayoutMeasure> measures)
		{
			var widest = 0f;

			for (var start = 0; start < measures.Count; start += m_maxPerLine) {
				var count = Mathf.Min(m_maxPerLine, measures.Count - start);
				widest = Mathf.Max(widest, LineSolver.MainTotals(measures, start, count, m_spacing).Preferred);
			}

			return widest;
		}

		private static float LargestOuterMin(List<LayoutMeasure> measures)
		{
			var largest = 0f;

			foreach (var measure in measures) {
				largest = Mathf.Max(largest, measure.OuterMin);
			}

			return largest;
		}

		private float PaddingStart(int axis)
		{
			return m_padding.Start(axis);
		}

		private float PaddingTotal(int axis)
		{
			return m_padding.Total(axis);
		}
	}
}
