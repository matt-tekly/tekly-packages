using NUnit.Framework;
using Tekly.Simulant.Core;

namespace Tekly.Simulant.Collections
{
	public class IndexArrayTests
	{
		[Test]
		public void BasicOperations()
		{
			const int DEFAULT = -1;
			
			var arr = new IndexArray(2, DEFAULT);
			
			Assert.That(arr.Data[0], Is.EqualTo(DEFAULT));
			Assert.That(arr.Data[1], Is.EqualTo(DEFAULT));

			arr.Add(0);
			Assert.That(arr.Data[0], Is.EqualTo(0));
			
			arr.Add(1);
			
			Assert.That(arr.Data[0], Is.EqualTo(0));
			Assert.That(arr.Data[1], Is.EqualTo(1));

			arr.Pop();
			
			Assert.That(arr.Data[1], Is.EqualTo(DEFAULT));
			
			arr.Add(1);
			arr.Add(2);
			arr.Add(3);
			
			Assert.That(arr.Data[0], Is.EqualTo(0));
			Assert.That(arr.Data[1], Is.EqualTo(1));
			Assert.That(arr.Data[2], Is.EqualTo(2));
			Assert.That(arr.Data[3], Is.EqualTo(3));
			
			arr.Pop();
			
			Assert.That(arr.Data[0], Is.EqualTo(0));
			Assert.That(arr.Data[1], Is.EqualTo(1));
			Assert.That(arr.Data[2], Is.EqualTo(2));
			Assert.That(arr.Data[3], Is.EqualTo(DEFAULT));
		}
	}
}