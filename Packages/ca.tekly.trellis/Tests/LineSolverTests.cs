using System.Collections.Generic;
using NUnit.Framework;

namespace Tekly.Trellis
{
	[TestFixture]
	public class LineSolverTests
	{
		private const float TOLERANCE = 0.001f;

		private LineSolver m_solver;
		private List<float> m_sizes;
		private List<float> m_positions;

		[SetUp]
		public void SetUp()
		{
			m_solver = new LineSolver();
			m_sizes = new List<float>();
			m_positions = new List<float>();
		}

		private void Solve(List<LayoutMeasure> items, float length, float spacing = 0f,
			LayoutAlignment alignment = LayoutAlignment.Start)
		{
			LineSolver.EnsureCount(m_sizes, items.Count);
			LineSolver.EnsureCount(m_positions, items.Count);
			m_solver.Solve(items, 0, items.Count, length, spacing, alignment, m_sizes, m_positions);
		}

		private void AssertSizes(params float[] expected)
		{
			for (var i = 0; i < expected.Length; i++) {
				Assert.AreEqual(expected[i], m_sizes[i], TOLERANCE, $"size {i}");
			}
		}

		private void AssertPositions(params float[] expected)
		{
			for (var i = 0; i < expected.Length; i++) {
				Assert.AreEqual(expected[i], m_positions[i], TOLERANCE, $"position {i}");
			}
		}

		[Test]
		public void ExactPreferredFitUsesPreferredSizes()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(10, 50),
				LayoutMeasure.Create(10, 30)
			};

			Solve(items, 80);

			AssertSizes(50, 30);
			AssertPositions(0, 50);
		}

		[Test]
		public void SqueezedBlendsBetweenMinAndPreferred()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 100),
				LayoutMeasure.Create(20, 60)
			};

			// Total min 20, total preferred 160, 90 available: halfway
			Solve(items, 90);

			AssertSizes(50, 40);
		}

		[Test]
		public void BelowMinStaysAtMinAndOverflows()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(40, 100),
				LayoutMeasure.Create(40, 100)
			};

			Solve(items, 50);

			AssertSizes(40, 40);
			AssertPositions(0, 40);
		}

		[Test]
		public void SpacingIsRemovedBeforeSizing()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 50),
				LayoutMeasure.Create(0, 50),
				LayoutMeasure.Create(0, 50)
			};

			Solve(items, 170, 10);

			AssertSizes(50, 50, 50);
			AssertPositions(0, 60, 120);
		}

		[TestCase(LayoutAlignment.Start, 0f)]
		[TestCase(LayoutAlignment.Center, 25f)]
		[TestCase(LayoutAlignment.End, 50f)]
		public void SpareWithoutFlexibleBecomesAlignmentOffset(LayoutAlignment alignment, float firstPosition)
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 30)
			};

			Solve(items, 100, 0, alignment);

			AssertSizes(20, 30);
			AssertPositions(firstPosition, firstPosition + 20);
		}

		[Test]
		public void FlexibleSharesSpareByWeight()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 10, 1),
				LayoutMeasure.Create(0, 10, 3),
				LayoutMeasure.Create(0, 10)
			};

			// 70 spare: 1/4 and 3/4
			Solve(items, 100, 0, LayoutAlignment.End);

			AssertSizes(27.5f, 62.5f, 10);
			AssertPositions(0, 27.5f, 90);
		}

		[Test]
		public void CappedItemPassesItsShareToTheRest()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 0, 1, 20),
				LayoutMeasure.Create(0, 0, 1)
			};

			Solve(items, 100);

			AssertSizes(20, 80);
		}

		[Test]
		public void CapsCascadeAcrossRounds()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 0, 1, 10),
				LayoutMeasure.Create(0, 0, 1, 50),
				LayoutMeasure.Create(0, 0, 1)
			};

			// Round 1: 40 each, first caps at 10. Round 2: 55 each, second caps at 50. Round 3: last gets 60.
			Solve(items, 120);

			AssertSizes(10, 50, 60);
		}

		[TestCase(LayoutAlignment.Start, 0f)]
		[TestCase(LayoutAlignment.Center, 30f)]
		[TestCase(LayoutAlignment.End, 60f)]
		public void AllCappedLeavesSpareForAlignment(LayoutAlignment alignment, float firstPosition)
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 0, 1, 20),
				LayoutMeasure.Create(0, 0, 1, 20)
			};

			Solve(items, 100, 0, alignment);

			AssertSizes(20, 20);
			AssertPositions(firstPosition, firstPosition + 20);
		}

		[Test]
		public void MaxLimitsPreferred()
		{
			var item = LayoutMeasure.Create(0, 100, 0, 40);

			Assert.AreEqual(40, item.Preferred, TOLERANCE);
		}

		[Test]
		public void MinWinsOverMax()
		{
			var item = LayoutMeasure.Create(50, 60, 0, 20);

			Assert.AreEqual(50, item.Min, TOLERANCE);
			Assert.AreEqual(50, item.Max, TOLERANCE);
			Assert.AreEqual(50, item.Preferred, TOLERANCE);
		}

		[Test]
		public void MarginsOffsetPositionsAndTakeSpace()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 20, 0, float.PositiveInfinity, 5, 10),
				LayoutMeasure.Create(0, 20, 1, float.PositiveInfinity, 3, 0)
			};

			// Outer preferred 35 + 23 = 58, plus 4 spacing, in 100: 38 spare to the flexible item
			Solve(items, 100, 4);

			AssertSizes(20, 58);
			AssertPositions(5, 42);
		}

		[Test]
		public void MainTotalsIncludeMarginsAndSpacing()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(10, 20, 1, float.PositiveInfinity, 2, 3),
				LayoutMeasure.Create(5, 15, 2)
			};

			var totals = LineSolver.MainTotals(items, 0, items.Count, 4);

			Assert.AreEqual(10 + 5 + 5 + 4, totals.Min, TOLERANCE);
			Assert.AreEqual(20 + 5 + 15 + 4, totals.Preferred, TOLERANCE);
			Assert.AreEqual(3, totals.Flexible, TOLERANCE);
		}

		[Test]
		public void CrossTotalsTakeTheLargestItem()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(10, 20, 1, float.PositiveInfinity, 2, 3),
				LayoutMeasure.Create(12, 22, 2)
			};

			var totals = LineSolver.CrossTotals(items, 0, items.Count);

			Assert.AreEqual(15, totals.Min, TOLERANCE);
			Assert.AreEqual(25, totals.Preferred, TOLERANCE);
			Assert.AreEqual(2, totals.Flexible, TOLERANCE);
		}

		[Test]
		public void SolvesASubRangeOnly()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 30),
				LayoutMeasure.Create(0, 10, 1),
				LayoutMeasure.Create(0, 10, 1)
			};

			LineSolver.EnsureCount(m_sizes, 3);
			LineSolver.EnsureCount(m_positions, 3);
			m_sizes[0] = -1;

			m_solver.Solve(items, 1, 2, 50, 0, LayoutAlignment.Start, m_sizes, m_positions);

			Assert.AreEqual(-1, m_sizes[0]);
			Assert.AreEqual(25, m_sizes[1], TOLERANCE);
			Assert.AreEqual(25, m_sizes[2], TOLERANCE);
			Assert.AreEqual(0, m_positions[1], TOLERANCE);
			Assert.AreEqual(25, m_positions[2], TOLERANCE);
		}

		[Test]
		public void EmptyLineDoesNothing()
		{
			Solve(new List<LayoutMeasure>(), 100);
			Assert.AreEqual(0, m_sizes.Count);
		}

		[TestCase(LayoutAlignment.Start, 0f)]
		[TestCase(LayoutAlignment.Center, 20f)]
		[TestCase(LayoutAlignment.End, 40f)]
		public void ShortLineMatchesFullLineColumns(LayoutAlignment alignment, float firstPosition)
		{
			// Full line of 3 flexible cards in 120 with 0 spacing: 40 each. Last line holds 2.
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 0, 1),
				LayoutMeasure.Create(0, 0, 1),
				LayoutMeasure.Create(0, 0, 1),
				LayoutMeasure.Create(0, 0, 1),
				LayoutMeasure.Create(0, 0, 1)
			};

			LineSolver.EnsureCount(m_sizes, items.Count);
			LineSolver.EnsureCount(m_positions, items.Count);

			m_solver.SolveShortLine(items, 3, 2, 0, 3, 120, 0, alignment, m_sizes, m_positions);

			Assert.AreEqual(40, m_sizes[3], TOLERANCE);
			Assert.AreEqual(40, m_sizes[4], TOLERANCE);
			Assert.AreEqual(firstPosition, m_positions[3], TOLERANCE);
			Assert.AreEqual(firstPosition + 40, m_positions[4], TOLERANCE);
		}

		[Test]
		public void ShortLineCountsSpacingForMissingSlots()
		{
			// 3 slots in 100 with 5 spacing: (100 - 10) / 3 = 30 each
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 0, 1),
				LayoutMeasure.Create(0, 0, 1),
				LayoutMeasure.Create(0, 0, 1),
				LayoutMeasure.Create(0, 0, 1)
			};

			LineSolver.EnsureCount(m_sizes, items.Count);
			LineSolver.EnsureCount(m_positions, items.Count);

			m_solver.SolveShortLine(items, 3, 1, 0, 3, 100, 5, LayoutAlignment.Start, m_sizes, m_positions);

			Assert.AreEqual(30, m_sizes[3], TOLERANCE);
			Assert.AreEqual(0, m_positions[3], TOLERANCE);
		}

		[Test]
		public void FullLineSolvesNormally()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 0, 1),
				LayoutMeasure.Create(0, 0, 1)
			};

			LineSolver.EnsureCount(m_sizes, items.Count);
			LineSolver.EnsureCount(m_positions, items.Count);

			m_solver.SolveShortLine(items, 0, 2, 0, 2, 100, 0, LayoutAlignment.Start, m_sizes, m_positions);

			AssertSizes(50, 50);
			AssertPositions(0, 50);
		}

		[Test]
		public void SpaceBetweenPutsEndsAtEdges()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 20)
			};

			// 40 spare over 2 gaps
			Solve(items, 100, 0, LayoutAlignment.SpaceBetween);

			AssertSizes(20, 20, 20);
			AssertPositions(0, 40, 80);
		}

		[Test]
		public void SpaceBetweenAddsToSpacing()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 20)
			};

			// 100 - 60 - 10 spacing = 30 spare, 15 extra per gap on top of 5 spacing
			Solve(items, 100, 5, LayoutAlignment.SpaceBetween);

			AssertPositions(0, 40, 80);
		}

		[Test]
		public void SpaceBetweenSingleItemSitsAtStart()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 20)
			};

			Solve(items, 100, 0, LayoutAlignment.SpaceBetween);

			AssertPositions(0);
		}

		[Test]
		public void SpaceEvenlyGivesEqualGaps()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 20)
			};

			// 40 spare over 4 gaps: 10 each
			Solve(items, 100, 0, LayoutAlignment.SpaceEvenly);

			AssertPositions(10, 40, 70);
		}

		[Test]
		public void SpaceEvenlySingleItemIsCentered()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 20)
			};

			Solve(items, 100, 0, LayoutAlignment.SpaceEvenly);

			AssertPositions(40);
		}

		[Test]
		public void SpreadDoesNothingWhenFlexibleTakesTheRoom()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 20, 1)
			};

			Solve(items, 100, 0, LayoutAlignment.SpaceBetween);

			AssertSizes(20, 80);
			AssertPositions(0, 20);
		}

		[Test]
		public void SpreadKeepsMarginsAndSpacing()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 20, 0, float.PositiveInfinity, 5, 5),
				LayoutMeasure.Create(0, 20, 0, float.PositiveInfinity, 5, 5)
			};

			// Outer 30 + 30 + 4 spacing = 64, 36 spare to the one gap
			Solve(items, 100, 4, LayoutAlignment.SpaceBetween);

			AssertPositions(5, 75);
		}

		[TestCase(LayoutAlignment.SpaceBetween)]
		[TestCase(LayoutAlignment.SpaceEvenly)]
		public void ShortLineUsesStartForSpread(LayoutAlignment alignment)
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 20),
				LayoutMeasure.Create(0, 20)
			};

			LineSolver.EnsureCount(m_sizes, items.Count);
			LineSolver.EnsureCount(m_positions, items.Count);

			m_solver.SolveShortLine(items, 3, 1, 0, 3, 100, 0, alignment, m_sizes, m_positions);

			Assert.AreEqual(0, m_positions[3], TOLERANCE);
		}

		[Test]
		public void CrossStretchFillsUpToMax()
		{
			LineSolver.SolveCross(LayoutMeasure.Create(0, 10, 0, 60, 5, 5), 100, CrossAlignment.Stretch,
				out var size, out var position);

			Assert.AreEqual(60, size, TOLERANCE);
			Assert.AreEqual(5, position, TOLERANCE);
		}

		[TestCase(CrossAlignment.Start, 0f)]
		[TestCase(CrossAlignment.Center, 40f)]
		[TestCase(CrossAlignment.End, 80f)]
		public void CrossAlignedUsesPreferred(CrossAlignment alignment, float expectedPosition)
		{
			LineSolver.SolveCross(LayoutMeasure.Create(0, 20), 100, alignment, out var size, out var position);

			Assert.AreEqual(20, size, TOLERANCE);
			Assert.AreEqual(expectedPosition, position, TOLERANCE);
		}

		[Test]
		public void CrossFlexibleStretchesEvenWhenAligned()
		{
			LineSolver.SolveCross(LayoutMeasure.Create(0, 20, 1), 100, CrossAlignment.Center,
				out var size, out var position);

			Assert.AreEqual(100, size, TOLERANCE);
			Assert.AreEqual(0, position, TOLERANCE);
		}

		[Test]
		public void CrossSqueezedStopsAtMin()
		{
			LineSolver.SolveCross(LayoutMeasure.Create(30, 50), 20, CrossAlignment.Start, out var size, out _);

			Assert.AreEqual(30, size, TOLERANCE);
		}
	}
}
