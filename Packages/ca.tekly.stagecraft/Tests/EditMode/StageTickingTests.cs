using System.Collections.Generic;
using NUnit.Framework;

namespace Tekly.Stagecraft.Tests
{
	public class StageTickingTests : StageTestBase
	{
		[Test]
		public void DerivedRegisteredType_TicksOnce_InItsOwnGroup()
		{
			var instance = Stage.Spawn(CreatePrefab<DerivedRegistered>());
			Stage.Tick(0f);
			Stage.Tick(0f);
			Stage.FixedTick(0f);

			// Once per tick: it is in exactly one group, not in both its own and its base's
			Assert.AreEqual(2, instance.Ticks);
			Assert.AreEqual(1, instance.FixedTicks);
			Assert.AreEqual(typeof(DerivedRegistered), instance.TypeInfo.Group.Type);
			Assert.AreEqual(0, Stage.All<TickBehaviour>().RawCount, "not in the base group");
			Assert.AreEqual(1, Stage.All<DerivedRegistered>().RawCount);
		}

		[Test]
		public void DerivedUnregisteredType_TicksOnce_InTheBaseGroup()
		{
			var instance = Stage.Spawn(CreatePrefab<DerivedUnregistered>());
			Stage.Tick(0f);
			Stage.Tick(0f);

			Assert.AreEqual(2, instance.Ticks);
			Assert.AreEqual(typeof(TickBehaviour), instance.TypeInfo.Group.Type);
			Assert.AreEqual(1, Stage.All<TickBehaviour>().RawCount);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void NonOverridingType_IsNeverScheduled()
		{
			var instance = Stage.Spawn(CreatePrefab<PlainBehaviour>());
			Stage.Tick(0f);

			Assert.IsFalse(instance.TypeInfo.Ticks);
			Assert.IsFalse(instance.TypeInfo.FixedTicks);
			Assert.AreEqual(-1, instance.TickIndex);
			Assert.AreEqual(-1, instance.FixedIndex);
			Assert.AreEqual(0, instance.TypeInfo.Group.TickCount);
			Assert.AreEqual(1, instance.TypeInfo.Group.RawCount, "still queryable");
		}

		[Test]
		public void UnregisteredTickingType_LogsOnce_AndStillTicksLast()
		{
			ExpectErrors();
			var prefab = CreatePrefab<UnregisteredTicker>();
			var order = new List<string>();

			var registered = Stage.Spawn(CreatePrefab<TickBehaviour>());
			registered.OnTick = _ => order.Add("registered");

			var a = Stage.Spawn(prefab);
			var b = Stage.Spawn(prefab);
			Stage.Tick(0f);

			Assert.AreEqual(1, Errors, "once per type, not per instance");
			StringAssert.Contains(nameof(UnregisteredTicker), LastError);

			Assert.AreEqual(1, a.Ticks);
			Assert.AreEqual(1, b.Ticks);
			Assert.AreSame(Stage.FallbackGroup, a.TypeInfo.Group);
			Assert.AreEqual(new[] { "registered" }, order);
		}

		[Test]
		public void Groups_TickInRegistrationOrder()
		{
			var order = new List<string>();
			var first = Stage.Spawn(CreatePrefab<TickBehaviour>());
			var second = Stage.Spawn(CreatePrefab<DerivedRegistered>());
			first.OnTick = _ => order.Add("first");
			second.OnTick = _ => order.Add("second");

			Stage.Tick(0f);
			Assert.AreEqual(new[] { "first", "second" }, order);
		}

		[Test]
		public void SwapRemove_KeepsListsConsistent_UnderMassDespawn()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var all = new List<TickBehaviour>();
			for (var i = 0; i < 100; i++) {
				all.Add(Stage.Spawn(prefab));
			}

			Stage.Tick(0f);

			var survivors = new List<TickBehaviour>();
			for (var i = 0; i < all.Count; i++) {
				if (i % 2 == 0) {
					all[i].Despawn();
				} else {
					survivors.Add(all[i]);
				}
			}

			Stage.Tick(0f);
			Stage.Tick(0f);

			var list = Stage.All<TickBehaviour>();
			Assert.AreEqual(50, list.RawCount);

			var seen = new HashSet<TickBehaviour>();
			foreach (var behaviour in list) {
				Assert.IsTrue(seen.Add(behaviour), "no duplicates");
				Assert.IsTrue(survivors.Contains(behaviour));
			}

			Assert.AreEqual(50, seen.Count);

			var span = list.AsSpan();
			foreach (var survivor in survivors) {
				Assert.AreEqual(3, survivor.Ticks);
				Assert.AreSame(survivor, span[survivor.AllIndex], "stored index matches the slot");
				Assert.GreaterOrEqual(survivor.TickIndex, 0);
			}

			foreach (var dead in all) {
				if (!survivors.Contains(dead)) {
					Assert.AreEqual(1, dead.Ticks);
					Assert.AreEqual(-1, dead.AllIndex);
					Assert.AreEqual(-1, dead.TickIndex);
				}
			}

			for (var i = 0; i < 50; i++) {
				Stage.Spawn(prefab);
			}

			Stage.Tick(0f);
			Assert.AreEqual(100, Stage.All<TickBehaviour>().RawCount);
			Assert.AreEqual(100, Stage.AliveCount);
		}

		[Test]
		public void All_OnUnregisteredType_IsEmptyAndLogs()
		{
			ExpectErrors();
			var list = Stage.All<UnregisteredTicker>();
			Assert.AreEqual(0, list.RawCount);
			Assert.AreEqual(1, Errors);

			var count = 0;
			foreach (var _ in list) {
				count++;
			}

			Assert.AreEqual(0, count);
		}

		[Test]
		public void System_GetsAddedRemoved_AndListSkipsDead()
		{
			var prefab = CreatePrefab<SystemBehaviour>();
			var a = Stage.Spawn(prefab);
			var b = Stage.Spawn(prefab);

			Assert.AreEqual(0, System.Added.Count, "added at sync, not at spawn");
			Stage.Tick(0f);
			CollectionAssert.AreEquivalent(new[] { a, b }, System.Added);
			Assert.AreEqual(1, System.TickCalls);
			Assert.AreEqual(2, System.LastLiveCount);
			Assert.AreSame(Stage, System.Stage);

			// Despawn b inside the system's tick: enumeration skips it from then on; the raw count drops at the sync
			System.OnTick = list => {
				if (b.IsAlive) {
					b.Despawn();
					var live = 0;
					foreach (var _ in list) {
						live++;
					}

					Assert.AreEqual(1, live);
					Assert.AreEqual(2, list.RawCount);
				}
			};

			Stage.Tick(0f);
			Assert.AreEqual(new[] { b }, System.Removed);

			Stage.Tick(0f);
			Assert.AreEqual(1, System.LastRawCount);
			Assert.AreEqual(3, a.Value);
			Assert.AreEqual(2, b.Value);
		}

		[Test]
		public void System_OnRemoved_RunsBeforeOnDespawned()
		{
			var order = new List<string>();
			var prefabActor = CreatePrefab("both", typeof(SystemBehaviour), typeof(TickBehaviour));
			var actor = Stage.Spawn(prefabActor);
			actor.Get<TickBehaviour>().OnDespawn = _ => order.Add("despawned");
			Stage.Tick(0f);

			actor.Despawn();
			var removedBefore = System.Removed.Count;
			Stage.Tick(0f);

			Assert.AreEqual(removedBefore + 1, System.Removed.Count);
			Assert.AreEqual(new[] { "despawned" }, order);
		}

		[Test]
		public void FixedTick_RunsLogic_ThenStep_ThenDeliver_ThenSync()
		{
			var physics = new FakePhysics();
			Stage.Physics = physics;

			var logger = Stage.Spawn(CreatePrefab<PhaseLogger>());
			logger.Log = physics.Log;
			Stage.Tick(0f);

			physics.OnDeliver = () => logger.Despawn();

			Stage.FixedTick(0.02f);

			Assert.AreEqual(new[] { "fixed", "step", "deliver", "despawned" }, physics.Log);
			Assert.AreEqual(ActorState.Pooled, logger.Actor.State, "a despawn from event delivery is finished before FixedTick returns");
		}

		[Test]
		public void FixedTickBeginEnd_LetsSomeoneElseStep()
		{
			var physics = new FakePhysics();
			Stage.Physics = physics;

			var logger = Stage.Spawn(CreatePrefab<PhaseLogger>());
			logger.Log = physics.Log;
			Stage.Tick(0f);

			Assert.IsTrue(Stage.FixedTickBegin(0.02f));
			physics.Log.Add("external step");
			Stage.FixedTickEnd();

			Assert.AreEqual(new[] { "fixed", "external step", "deliver" }, physics.Log);
			Assert.IsFalse(Stage.IsTicking);
		}

		[Test]
		public void FixedTick_Paused_SkipsStepButDelivers()
		{
			var physics = new FakePhysics();
			Stage.Physics = physics;
			Stage.Time.IsPaused = true;

			Stage.FixedTick(0.02f);

			Assert.AreEqual(new[] { "deliver" }, physics.Log);
			Assert.AreEqual(0, Stage.Time.FixedStep);
		}

		[Test]
		public void Config_RejectsDuplicateRegistration()
		{
			Assert.Throws<System.ArgumentException>(() => new StageConfig().Register<TickBehaviour>().Register<TickBehaviour>());
		}
	}
}
