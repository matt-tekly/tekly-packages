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

		private class QueryListener : IQueryListener
		{
			public int Count;

			public void EntityAdded(int entity, Query query) => Count++;
			public void EntityRemoved(int entity, Query query) => Count--;
		}
		
		[Test]
		public void Listeners()
		{
			const int ENTITY_A = 0;
			
			var query = new Query(Array.Empty<int>(), Array.Empty<int>(), 16, 16);
			var queryListener = new QueryListener();
			query.AddListener(queryListener);
			
			query.Add(ENTITY_A);
			Assert.That(queryListener.Count, Is.EqualTo(1));
			
			query.Remove(ENTITY_A);
			Assert.That(queryListener.Count, Is.EqualTo(0));
			
			query.RemoveListener(queryListener);
			
			query.Add(ENTITY_A);
			Assert.That(queryListener.Count, Is.EqualTo(0));
			
			query.Remove(ENTITY_A);
			Assert.That(queryListener.Count, Is.EqualTo(0));
		}
		
		[Test]
		public void DelegateListeners()
		{
			const int ENTITY_A = 0;

			int count = 0;
			var query = new Query(Array.Empty<int>(), Array.Empty<int>(), 16, 16);
			var disposable = query.Listen(_ => count++, _ => count--);
			
			query.Add(ENTITY_A);
			Assert.That(count, Is.EqualTo(1));
			
			query.Remove(ENTITY_A);
			Assert.That(count, Is.EqualTo(0));
			
			disposable.Dispose();
			
			query.Add(ENTITY_A);
			Assert.That(count, Is.EqualTo(0));
			
			query.Remove(ENTITY_A);
			Assert.That(count, Is.EqualTo(0));
		}
	}
}