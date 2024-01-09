using NUnit.Framework;

namespace Tekly.Simulant.Core
{
	public class DataPoolTests
	{
		private struct Identity
		{
			public string Id;
		}
        
		[Test]
		public void AddAndGet()
		{
			var worldConfig = new WorldConfig {
				EntityCapacity = 2,
				DataPools = new DataPoolConfig {
					Capacity = 2,
					RecycleCapacity = 2
				}
			};

			var world = new World(worldConfig);
			var pool = world.GetPool<Identity>();

			var entity = world.Create();
			
			ref var identity = ref pool.Add(entity);
			identity.Id = "Test Id 0";
			
			var retrievedIdentity = pool.Get(entity);
			
			Assert.That(identity.Id, Is.EqualTo(retrievedIdentity.Id));
		}
	}
}