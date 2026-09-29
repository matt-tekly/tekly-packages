using System.Collections.Generic;
using NUnit.Framework;

namespace Tekly.Trellis
{
	[TestFixture]
	public class LineBreakerTests
	{
		private readonly List<LayoutLine> m_lines = new List<LayoutLine>();

		private static List<LayoutMeasure> Items(params float[] preferred)
		{
			var items = new List<LayoutMeasure>();

			foreach (var size in preferred) {
				items.Add(LayoutMeasure.Create(0, size));
			}

			return items;
		}

		private void AssertLines(params int[] counts)
		{
			Assert.AreEqual(counts.Length, m_lines.Count, "line count");

			var start = 0;

			for (var i = 0; i < counts.Length; i++) {
				Assert.AreEqual(start, m_lines[i].Start, $"line {i} start");
				Assert.AreEqual(counts[i], m_lines[i].Count, $"line {i} count");
				start += counts[i];
			}
		}

		[Test]
		public void EverythingFitsOnOneLine()
		{
			LineBreaker.Break(Items(30, 30, 30), 100, 5, m_lines);
			AssertLines(3);
		}

		[Test]
		public void BreaksWhenNextItemWontFit()
		{
			LineBreaker.Break(Items(40, 40, 40, 40, 40), 100, 0, m_lines);
			AssertLines(2, 2, 1);
		}

		[Test]
		public void SpacingCountsTowardsTheLine()
		{
			// 30 + 10 + 30 + 10 + 30 = 110 > 100
			LineBreaker.Break(Items(30, 30, 30), 100, 10, m_lines);
			AssertLines(2, 1);
		}

		[Test]
		public void ExactFitStaysOnTheLine()
		{
			LineBreaker.Break(Items(50, 50), 100, 0, m_lines);
			AssertLines(2);
		}

		[Test]
		public void OversizedItemGetsItsOwnLine()
		{
			LineBreaker.Break(Items(20, 150, 20), 100, 0, m_lines);
			AssertLines(1, 1, 1);
		}

		[Test]
		public void MarginsCountTowardsTheLine()
		{
			var items = new List<LayoutMeasure> {
				LayoutMeasure.Create(0, 40, 0, float.PositiveInfinity, 5, 5),
				LayoutMeasure.Create(0, 40, 0, float.PositiveInfinity, 5, 5)
			};

			LineBreaker.Break(items, 90, 0, m_lines);
			AssertLines(1, 1);
		}

		[Test]
		public void ZeroLengthPutsEachItemOnItsOwnLine()
		{
			LineBreaker.Break(Items(10, 10), 0, 0, m_lines);
			AssertLines(1, 1);
		}

		[Test]
		public void NoItemsNoLines()
		{
			LineBreaker.Break(Items(), 100, 0, m_lines);
			AssertLines();
		}

		[Test]
		public void MaxPerLineStartsNewLines()
		{
			LineBreaker.Break(Items(10, 10, 10, 10, 10, 10, 10), 1000, 0, m_lines, 3);
			AssertLines(3, 3, 1);
		}

		[Test]
		public void LengthStillBreaksBeforeMaxPerLine()
		{
			LineBreaker.Break(Items(40, 40, 40, 40), 100, 0, m_lines, 3);
			AssertLines(2, 2);
		}

		[Test]
		public void SingleMakesOneLine()
		{
			LineBreaker.Single(4, m_lines);
			AssertLines(4);

			LineBreaker.Single(0, m_lines);
			AssertLines();
		}
	}
}
