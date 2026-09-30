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
			SharedSize.Combine(10, 30, 120, float.PositiveInfinity, out var min, out var preferred);

			Assert.AreEqual(30, min, TOLERANCE);
			Assert.AreEqual(120, preferred, TOLERANCE);
		}

		[Test]
		public void CapLimitsTheSharedSize()
		{
			SharedSize.Combine(10, 30, 120, 80, out var min, out var preferred);

			Assert.AreEqual(30, min, TOLERANCE);
			Assert.AreEqual(80, preferred, TOLERANCE);
		}

		[Test]
		public void CapLimitsTheSharedMinToo()
		{
			SharedSize.Combine(10, 100, 150, 80, out var min, out var preferred);

			Assert.AreEqual(80, min, TOLERANCE);
			Assert.AreEqual(80, preferred, TOLERANCE);
		}

		[Test]
		public void OwnMinWinsOverTheCap()
		{
			SharedSize.Combine(90, 90, 150, 80, out var min, out var preferred);

			Assert.AreEqual(90, min, TOLERANCE);
			Assert.AreEqual(90, preferred, TOLERANCE);
		}
	}
}
