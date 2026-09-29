using NUnit.Framework;

namespace Tekly.Trellis
{
	[TestFixture]
	public class EdgesTests
	{
		[Test]
		public void TotalsAndSidesPerAxis()
		{
			var edges = new Edges(1, 2, 3, 4);

			Assert.AreEqual(3, edges.Horizontal);
			Assert.AreEqual(7, edges.Vertical);

			Assert.AreEqual(1, edges.Start(0));
			Assert.AreEqual(2, edges.End(0));
			Assert.AreEqual(3, edges.Total(0));

			Assert.AreEqual(3, edges.Start(1));
			Assert.AreEqual(4, edges.End(1));
			Assert.AreEqual(7, edges.Total(1));
		}

		[Test]
		public void FactoriesFillSides()
		{
			Assert.AreEqual(new Edges(5, 5, 5, 5), Edges.All(5));
			Assert.AreEqual(new Edges(2, 2, 8, 8), Edges.Symmetric(2, 8));
		}

		[Test]
		public void EqualityComparesAllSides()
		{
			Assert.IsTrue(new Edges(1, 2, 3, 4) == new Edges(1, 2, 3, 4));
			Assert.IsTrue(new Edges(1, 2, 3, 4) != new Edges(1, 2, 3, 5));
			Assert.IsFalse(new Edges(1, 2, 3, 4).Equals(new Edges(0, 2, 3, 4)));
		}
	}
}
