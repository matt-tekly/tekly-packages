using NUnit.Framework;
using Tekly.Simulant.Core;

namespace Tekly.Simulant.Collections
{
	public class GrowingArrayTests
	{
		[Test]
		public void Add()
		{
			var growingArray = new GrowingArray<int>(2);

			var index1 = growingArray.Add(1);
			var index2 = growingArray.Add(2);
			
			Assert.That(growingArray.Count == 2);
			
			var index3 = growingArray.Add(3);
			
			Assert.That(growingArray.Count == 3);
			
			Assert.That(growingArray.Data[index1], Is.EqualTo(1));
			Assert.That(growingArray.Data[index2], Is.EqualTo(2));
			Assert.That(growingArray.Data[index3], Is.EqualTo(3));
		}
	}
}