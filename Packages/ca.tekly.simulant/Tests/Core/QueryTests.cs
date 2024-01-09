using System;
using NUnit.Framework;

namespace Tekly.Simulant.Core
{
	public class QueryTests
	{
		[Test]
		public void AddRemove()
		{
			var query = new Query(Array.Empty<int>(), Array.Empty<int>(), 16, 16);
			
			query.Add(0);
			query.Add(1);
			
			Assert.That(query.Count, Is.EqualTo(2));
			CollectionAssert.Contains(query.GetRawEntities().Data, 0);
			CollectionAssert.Contains(query.GetRawEntities().Data, 1);
			
			query.Remove(0);
			
			Assert.That(query.Count, Is.EqualTo(1));
			CollectionAssert.DoesNotContain(query.GetRawEntities().Data, 0);
		}
	}
}