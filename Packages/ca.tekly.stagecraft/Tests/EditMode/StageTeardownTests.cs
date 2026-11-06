using NUnit.Framework;

namespace Tekly.Stagecraft.Tests
{
	public class StageTeardownTests : StageTestBase
	{
		[Test]
		public void Clear_DespawnsActorsSpawnedThisFrame()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var ticked = Stage.Spawn(prefab);
			Stage.Tick(0f);
			var fresh = Stage.Spawn(prefab);

			Stage.Clear();

			Assert.AreEqual(0, Stage.AliveCount);
			Assert.AreEqual(0, Stage.RegisteredCount);
			Assert.AreEqual(1, ticked.Despawned);
			Assert.AreEqual(1, fresh.Despawned);
			Assert.AreEqual(ActorState.Pooled, fresh.Actor.State);
			Assert.AreEqual(0, Stage.All<TickBehaviour>().RawCount);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void Clear_DespawnsActorsSpawnedByOnDespawned()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var effectPrefab = CreatePrefab<TickBehaviour>("effect");

			TickBehaviour effect = null;
			var instance = Stage.Spawn(prefab);
			instance.OnDespawn = _ => effect = Stage.Spawn(effectPrefab);
			Stage.Tick(0f);

			Stage.Clear();

			Assert.IsNotNull(effect, "the death effect was spawned");
			Assert.AreEqual(1, effect.Despawned, "and cleared in a later pass");
			Assert.AreEqual(0, Stage.AliveCount);
			Assert.AreEqual(0, Stage.RegisteredCount);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void Clear_ReleasesAcquiredInstances()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var acquired = Stage.Acquire(prefab);

			Stage.Clear();

			Assert.AreEqual(ActorState.Pooled, acquired.Actor.State);
			Assert.AreEqual(0, acquired.Despawned);
		}

		[Test]
		public void Clear_DuringTick_IsDeferredToTheEndOfTheTick()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var a = Stage.Spawn(prefab);
			var b = Stage.Spawn(prefab);
			Stage.Tick(0f);

			var bTicksWhenCleared = -1;
			a.OnTick = _ => {
				Stage.Clear();
				bTicksWhenCleared = b.Ticks;
				Assert.IsTrue(b.IsAlive, "nothing happens mid-tick");
			};

			Stage.Tick(0f);

			Assert.AreEqual(0, Stage.AliveCount);
			Assert.AreEqual(1, a.Despawned);
			Assert.AreEqual(1, b.Despawned);
			Assert.AreEqual(2, b.Ticks, "b still ticked this frame");
			Assert.IsFalse(Stage.IsTicking);
		}

		[Test]
		public void Clear_OnEmptyStage_IsFine()
		{
			Stage.Clear();
			Stage.Clear();
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void Dispose_DestroysPoolsAndRoot()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var instance = Stage.Spawn(prefab);
			var root = Stage.Root;
			var go = instance.gameObject;

			Stage.Dispose();

			Assert.IsTrue(Stage.IsDisposed);
			Assert.AreEqual(1, instance.Despawned);
			Assert.IsTrue(root == null, "root destroyed");
			Assert.IsTrue(go == null, "instances destroyed");
			Assert.Throws<System.ObjectDisposedException>(() => Stage.Spawn(prefab));

			Stage.Dispose();
			Stage.Tick(0f);
			Assert.AreEqual(0, Errors);
		}
	}
}
