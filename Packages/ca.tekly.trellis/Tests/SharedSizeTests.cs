using NUnit.Framework;

namespace Tekly.Trellis
{
	[TestFixture]
	public class SharedSizeTests
	{
		private const float TOLERANCE = 0.001f;

		[Test]
		public void TakesTheGroupsLargestWithoutACap()
		{
			SharedSize.Combine(10, 30, 120, 0, float.PositiveInfinity, out var min, out var preferred);

			Assert.AreEqual(30, min, TOLERANCE);
			Assert.AreEqual(120, preferred, TOLERANCE);
		}

		[Test]
		public void CapLimitsTheSharedSize()
		{
			SharedSize.Combine(10, 30, 120, 0, 80, out var min, out var preferred);

			Assert.AreEqual(30, min, TOLERANCE);
			Assert.AreEqual(80, preferred, TOLERANCE);
		}

		[Test]
		public void CapLimitsTheSharedMinToo()
		{
			SharedSize.Combine(10, 100, 150, 0, 80, out var min, out var preferred);

			Assert.AreEqual(80, min, TOLERANCE);
			Assert.AreEqual(80, preferred, TOLERANCE);
		}

		[Test]
		public void FloorRaisesShortColumns()
		{
			SharedSize.Combine(10, 30, 60, 100, float.PositiveInfinity, out var min, out var preferred);

			Assert.AreEqual(100, min, TOLERANCE);
			Assert.AreEqual(100, preferred, TOLERANCE);
		}

		[Test]
		public void FloorLeavesWiderColumnsAlone()
		{
			SharedSize.Combine(10, 30, 150, 100, float.PositiveInfinity, out var min, out var preferred);

			Assert.AreEqual(100, min, TOLERANCE);
			Assert.AreEqual(150, preferred, TOLERANCE);
		}

		[Test]
		public void FloorWinsOverTheCap()
		{
			SharedSize.Combine(10, 30, 150, 100, 80, out var min, out var preferred);

			Assert.AreEqual(100, min, TOLERANCE);
			Assert.AreEqual(100, preferred, TOLERANCE);
		}

		[Test]
		public void OwnMinWinsOverTheCap()
		{
			SharedSize.Combine(90, 90, 150, 0, 80, out var min, out var preferred);

			Assert.AreEqual(90, min, TOLERANCE);
			Assert.AreEqual(90, preferred, TOLERANCE);
		}
	}
}
