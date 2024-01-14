using NUnit.Framework;

namespace Tekly.Common.Collections
{
	public class FixedRingTests
	{
		[Test]
		public void SimpleTest()
		{
			var ring = new FixedRing<int>(3);
			
			ring.Add(1);
			ring.Add(2);
			ring.Add(3);

			int sum = 0;
			foreach (var item in ring) {
				sum += item;
			}
			
			Assert.That(sum, Is.EqualTo(1+2+3));
			
			ring.Add(4);
			
			sum = 0;
			foreach (var item in ring) {
				sum += item;
			}
			
			Assert.That(sum, Is.EqualTo(2+3+4));
		}
	}
}