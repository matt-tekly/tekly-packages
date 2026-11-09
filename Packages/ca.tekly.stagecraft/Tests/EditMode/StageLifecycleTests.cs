using NUnit.Framework;
using UnityEngine;

namespace Tekly.Stagecraft.Tests
{
	public class StageLifecycleTests : StageTestBase
	{
		[Test]
		public void Spawn_IsAliveNow_AndTicksAfterSync()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var instance = Stage.Spawn(prefab, new Pose(new Vector3(1, 2, 3), Quaternion.identity));

			Assert.AreEqual(ActorState.Active, instance.Actor.State);
			Assert.IsTrue(instance.IsAlive);
			Assert.IsFalse(instance.Id.IsNone);
			Assert.AreEqual(1, instance.Spawned);
			Assert.AreEqual(1, Stage.AliveCount);
			Assert.AreEqual(new Vector3(1, 2, 3), instance.transform.position);
			Assert.IsTrue(instance.gameObject.activeSelf);
			Assert.IsTrue(Stage.TryGet(instance.Id, out var actor));
			Assert.AreSame(instance.Actor, actor);

			Assert.IsFalse(instance.Actor.IsScheduled, "joins the tick lists at the next sync");

			Stage.Tick(0.1f);

			Assert.IsTrue(instance.Actor.IsScheduled);
			Assert.AreEqual(1, instance.Ticks);
			Assert.AreEqual(0.1f, instance.LastDelta);
		}

		[Test]
		public void Spawn_GivesUniqueIds_AndReusedSlotsGetNewGenerations()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var a = Stage.Spawn(prefab);
			var b = Stage.Spawn(prefab);
			Assert.AreNotEqual(a.Id, b.Id);

			var oldId = a.Id;
			a.Despawn();
			Stage.Tick(0f);

			var c = Stage.Spawn(prefab);
			Assert.AreSame(a, c, "the pooled instance is reused");
			Assert.AreNotEqual(oldId, c.Id);
			Assert.AreEqual(oldId.Index, c.Id.Index);
			Assert.IsFalse(Stage.TryGet(oldId, out _));
		}

		[Test]
		public void Spawn_ReturnsTheMatchingInstanceBehaviour()
		{
			var prefabActor = CreatePrefab("multi", typeof(PlainBehaviour), typeof(TickBehaviour), typeof(ArgsBehaviour));
			var prefabTick = prefabActor.GetComponent<TickBehaviour>();

			var instance = Stage.Spawn(prefabTick);

			Assert.AreNotSame(prefabTick, instance);
			Assert.AreEqual(3, instance.Actor.Behaviours.Length);
			Assert.AreSame(instance, instance.Actor.Behaviours[1]);
		}

		[Test]
		public void Args_ArriveBeforeOnSpawned()
		{
			var prefab = CreatePrefab<ArgsBehaviour>();
			var instance = Stage.Spawn(prefab, Pose.identity, new TestArgs { Number = 7, Speed = 2f });

			Assert.AreEqual(7, instance.Args.Number);
			Assert.AreEqual(2f, instance.Args.Speed);
			Assert.IsTrue(instance.ArgsSeenInOnSpawned);
			Assert.AreEqual(1, instance.Initialized);
		}

		[Test]
		public void Despawn_IsDeferred()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var instance = Stage.Spawn(prefab);
			Stage.Tick(0f);

			var id = instance.Id;
			instance.Despawn();

			Assert.AreEqual(ActorState.Despawning, instance.Actor.State);
			Assert.IsFalse(instance.IsAlive);
			Assert.AreEqual(0, Stage.AliveCount);
			Assert.IsFalse(Stage.TryGet(id, out _), "the id is dead immediately");
			Assert.AreEqual(0, instance.Despawned, "OnDespawned waits for the sync point");
			Assert.IsTrue(instance.gameObject.activeSelf, "deactivation waits for the sync point");

			Stage.Tick(0f);

			Assert.AreEqual(1, instance.Despawned);
			Assert.AreEqual(ActorState.Pooled, instance.Actor.State);
			Assert.IsTrue(instance.Id.IsNone);
			Assert.IsFalse(instance.gameObject.activeSelf);
			Assert.AreEqual(1, instance.Ticks, "not ticked after despawn");
			Assert.AreEqual(0, Stage.RegisteredCount);
		}

		[Test]
		public void Despawn_Twice_IsOneDespawn_AndPoolsOnce()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var instance = Stage.Spawn(prefab);

			instance.Despawn();
			instance.Despawn();
			Stage.Despawn(instance.Actor);

			Assert.AreEqual(0, Stage.AliveCount);
			Stage.Tick(0f);
			Assert.AreEqual(1, instance.Despawned);
			Assert.AreEqual(ActorState.Pooled, instance.Actor.State);

			// Pushed into the pool exactly once: two spawns must give two different objects
			var a = Stage.Spawn(prefab);
			var b = Stage.Spawn(prefab);
			Assert.AreNotSame(a, b);
		}

		[Test]
		public void Despawn_StaleId_IsFalse_AndDoesNotTouchTheNewOccupant()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var first = Stage.Spawn(prefab);
			var staleId = first.Id;
			first.Despawn();
			Stage.Tick(0f);

			var second = Stage.Spawn(prefab);
			Assert.AreSame(first, second);

			Assert.IsFalse(Stage.Despawn(staleId));
			Assert.IsTrue(second.IsAlive);
			Assert.AreEqual(1, Stage.AliveCount);
		}

		[Test]
		public void Despawn_PooledActor_IsNoOp()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var instance = Stage.Spawn(prefab);
			instance.Despawn();
			Stage.Tick(0f);

			instance.Despawn();
			instance.Despawn();

			Assert.AreEqual(ActorState.Pooled, instance.Actor.State);
			Assert.AreEqual(0, Stage.PendingRemoveCount);
		}

		[Test]
		public void Despawn_AcquiredActor_ReturnsToPoolWithoutCallbacks()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var instance = Stage.Acquire(prefab);

			Assert.AreEqual(ActorState.Acquired, instance.Actor.State);
			Assert.IsFalse(instance.gameObject.activeSelf);
			Assert.IsTrue(instance.Id.IsNone);

			instance.Despawn();

			Assert.AreEqual(ActorState.Pooled, instance.Actor.State);
			Assert.AreEqual(0, instance.Spawned);
			Assert.AreEqual(0, instance.Despawned);
			Assert.AreEqual(0, Stage.AliveCount);
		}

		[Test]
		public void AcquireActivate_ExternalCreatorPath()
		{
			var prefab = CreatePrefab<ArgsBehaviour>();
			var instance = Stage.Acquire(prefab);

			Assert.IsFalse(Stage.TryGet(instance.Id, out _));

			Stage.Activate(instance, new Pose(Vector3.one, Quaternion.identity), new TestArgs { Number = 3 });

			Assert.IsTrue(instance.IsAlive);
			Assert.AreEqual(3, instance.Args.Number);
			Assert.AreEqual(Vector3.one, instance.transform.position);

			// Activating again is a no-op
			Stage.Activate(instance.Actor, Pose.identity);
			Assert.AreEqual(Vector3.one, instance.transform.position);
			Assert.AreEqual(1, Stage.AliveCount);
		}

		[Test]
		public void OnCreated_RunsOncePerInstance_BeforeTheFirstSpawn()
		{
			var prefab = CreatePrefab<TickBehaviour>();

			var instance = Stage.Spawn(prefab);
			Assert.AreEqual(1, instance.Created);
			Assert.IsFalse(instance.WasActiveInCreated, "created on the inactive object");
			Assert.IsFalse(instance.SpawnedBeforeCreated, "before the first OnSpawned");

			instance.Despawn();
			Stage.Tick(0f);
			var again = Stage.Spawn(prefab);

			Assert.AreSame(instance, again);
			Assert.AreEqual(1, again.Created, "not again on reuse");
			Assert.AreEqual(2, again.Spawned);

			var acquired = Stage.Acquire(prefab);
			Assert.AreEqual(1, acquired.Created, "Acquire creates the instance, so OnCreated has run before Activate");
		}

		[Test]
		public void Activate_WithRestoredReason_IsVisibleInOnSpawned()
		{
			var prefab = CreatePrefab<TickBehaviour>();

			var spawned = Stage.Spawn(prefab);
			Assert.AreEqual(SpawnReason.Spawned, spawned.LastSpawnReason);
			Assert.AreEqual(SpawnReason.Spawned, spawned.Actor.SpawnReason);

			// A restore: acquire, set state, then activate with the reason
			var restored = Stage.Acquire(prefab);
			restored.Ticks = 42;
			Stage.Activate(restored.Actor, Pose.identity, SpawnReason.Restored);

			Assert.AreEqual(SpawnReason.Restored, restored.LastSpawnReason);
			Assert.AreEqual(42, restored.Ticks, "state set before Activate survives it");
			Assert.IsTrue(restored.IsAlive);

			// The reason belongs to the activation, not the instance: the next spawn of the same instance is a Spawned
			restored.Despawn();
			Stage.Tick(0f);
			var again = Stage.Spawn(prefab);
			Assert.AreSame(restored, again);
			Assert.AreEqual(SpawnReason.Spawned, again.LastSpawnReason);
		}

		[Test]
		public void Activate_PooledActor_IsAnError()
		{
			ExpectErrors();
			var prefab = CreatePrefab<TickBehaviour>();
			var instance = Stage.Spawn(prefab);
			instance.Despawn();
			Stage.Tick(0f);

			Stage.Activate(instance.Actor, Pose.identity);

			Assert.AreEqual(ActorState.Pooled, instance.Actor.State);
			Assert.AreEqual(1, Errors);
		}

		[Test]
		public void Despawn_InsideTick_IsNotTickedByLaterGroups_AndRefsGoNull()
		{
			// TickBehaviour's group ticks before DerivedRegistered's
			var killerPrefab = CreatePrefab<TickBehaviour>("killer");
			var victimPrefab = CreatePrefab<DerivedRegistered>("victim");

			var victim = Stage.Spawn(victimPrefab);
			var killer = Stage.Spawn(killerPrefab);
			Stage.Tick(0f);

			ActorRef<DerivedRegistered> victimRef = victim;
			var sawNullRef = false;

			killer.OnTick = _ => {
				victim.Despawn();
				sawNullRef = victimRef.Value == null;
			};

			Stage.Tick(0f);

			Assert.IsTrue(sawNullRef, "the ref reads null the moment Despawn is called");
			Assert.AreEqual(1, victim.Ticks, "only the tick from the previous frame");
			Assert.AreEqual(1, victim.Despawned, "cleaned up in the closing sync of the same tick");
			Assert.AreEqual(ActorState.Pooled, victim.Actor.State);
		}

		[Test]
		public void Spawn_FromOnDespawned_IsScheduledInTheSameSync()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var effectPrefab = CreatePrefab<TickBehaviour>("effect");

			var instance = Stage.Spawn(prefab);
			Stage.Tick(0f);

			TickBehaviour effect = null;
			instance.OnDespawn = _ => effect = Stage.Spawn(effectPrefab);

			instance.Despawn();
			Stage.Tick(0f);

			Assert.IsNotNull(effect);
			Assert.IsTrue(effect.IsAlive);
			Assert.IsTrue(effect.Actor.IsScheduled, "the next pass of the same sync scheduled it");
			Assert.AreEqual(1, Stage.AliveCount);
			Assert.AreEqual(0, Stage.PendingAddCount);

			Assert.AreEqual(1, effect.Ticks, "ticked by the tick whose opening sync scheduled it");
			Stage.Tick(0f);
			Assert.AreEqual(2, effect.Ticks);
		}

		[Test]
		public void Despawn_BeforeFirstSync_NeverTicks()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var instance = Stage.Spawn(prefab);
			instance.Despawn();

			Stage.Tick(0f);

			Assert.AreEqual(0, instance.Ticks);
			Assert.AreEqual(1, instance.Despawned);
			Assert.IsFalse(instance.Actor.IsScheduled);
			Assert.AreEqual(ActorState.Pooled, instance.Actor.State);
			Assert.AreEqual(0, Stage.All<TickBehaviour>().RawCount);
		}

		[Test]
		public void Sync_IsBounded_WhenCallbacksLoop()
		{
			ExpectErrors();
			var prefab = CreatePrefab<TickBehaviour>();

			// Every despawn spawns one that despawns itself immediately: an infinite spawn/despawn loop
			var looping = true;
			var instance = Stage.Spawn(prefab);
			instance.OnDespawn = self => {
				if (!looping) {
					return;
				}

				var next = Stage.Spawn(prefab);
				next.OnDespawn = self.OnDespawn;
				next.Despawn();
			};

			instance.Despawn();
			Stage.Tick(0f);

			Assert.GreaterOrEqual(Errors, 1);
			Assert.IsTrue(Stage.PendingRemoveCount > 0 || Stage.PendingAddCount > 0, "leftovers are kept, not lost");

			// Stop the loop so TearDown's Dispose drains the leftovers instead of hitting the bound again
			looping = false;
		}

		[Test]
		public void SpawnedAndDespawned_Events()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			Actor spawned = null;
			Actor despawned = null;
			ActorId despawnedId = default;

			using (Stage.Spawned.Subscribe(a => spawned = a))
			using (Stage.Despawned.Subscribe(a => {
				despawned = a;
				despawnedId = a.Id;
			})) {
				var instance = Stage.Spawn(prefab);
				Assert.AreSame(instance.Actor, spawned);

				var id = instance.Id;
				instance.Despawn();
				Assert.IsNull(despawned);

				Stage.Tick(0f);
				Assert.AreSame(instance.Actor, despawned);
				Assert.AreEqual(id, despawnedId, "the id is still readable in the event");
			}
		}

		[Test]
		public void Spawn_ByPrefabId()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var argsPrefab = CreatePrefab<ArgsBehaviour>();
			Stage.Prefabs.Register(new ActorPrefabId(10), prefab.GetComponent<Actor>());
			Stage.Prefabs.Register(new[] { argsPrefab.GetComponent<Actor>() }, 20);

			var actor = Stage.Spawn(new ActorPrefabId(10), Pose.identity);
			Assert.IsNotNull(actor.Get<TickBehaviour>());

			var withArgs = Stage.Spawn(new ActorPrefabId(20), Pose.identity, new TestArgs { Number = 5 });
			Assert.AreEqual(5, withArgs.Get<ArgsBehaviour>().Args.Number);

			Assert.IsTrue(Stage.Prefabs.TryGetId(argsPrefab.GetComponent<Actor>(), out var id));
			Assert.AreEqual(20, id.Value);
		}

		[Test]
		public void Injection_HappensOncePerInstance()
		{
			var prefab = CreatePrefab<InjectedBehaviour>();
			var instance = Stage.Spawn(prefab);
			Assert.IsNotNull(instance.Service);
			Assert.AreEqual(42, instance.Service.Value);

			var service = instance.Service;
			instance.Despawn();
			Stage.Tick(0f);

			var again = Stage.Spawn(prefab);
			Assert.AreSame(instance, again);
			Assert.AreSame(service, again.Service);
		}

		[Test]
		public void StaleSerializedBehaviours_LogOnceWhenThePoolIsCreated_AndAreNotRepaired()
		{
			ExpectErrors();

			// Collect ran with one behaviour; a second was added afterwards, as if OnValidate never saw it
			var prefab = CreatePrefab("Stale", typeof(TickBehaviour));
			prefab.gameObject.AddComponent<PlainBehaviour>();

			var instance = Stage.Spawn(prefab);
			Assert.AreEqual(1, Errors);
			StringAssert.Contains("Stale", LastError);
			Assert.AreEqual(1, instance.Behaviours.Length, "the stage trusts the serialized data");

			Stage.Spawn(prefab);
			Assert.AreEqual(1, Errors, "once per pool, not per instance");
		}

		[Test]
		public void Injection_RequiresHierarchyInjector()
		{
			var prefab = CreatePrefabWithoutInjector("NoInjector", typeof(InjectedBehaviour));
			var instance = Stage.Spawn(prefab).Get<InjectedBehaviour>();
			Assert.IsNull(instance.Service);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void Time_ScalesAndPauses()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var instance = Stage.Spawn(prefab);
			Stage.Tick(0f);

			Stage.Time.Scale = 0.5f;
			Stage.Tick(1f);
			Assert.AreEqual(0.5f, instance.LastDelta);
			Assert.AreEqual(0.5f, Stage.Time.Elapsed);

			Stage.Time.IsPaused = true;
			instance.Despawn();
			Stage.Tick(1f);

			Assert.AreEqual(2, instance.Ticks, "paused: no ticks");
			Assert.AreEqual(0.5f, Stage.Time.Elapsed);
			Assert.AreEqual(1, instance.Despawned, "paused: sync points still run");

			Stage.Time.IsPaused = false;
			Stage.FixedTick(0.02f);
			Assert.AreEqual(0.01f, Stage.Time.FixedDeltaTime, 1e-6f);
			Assert.AreEqual(1, Stage.Time.FixedStep);
		}

		[Test]
		public void Tick_Reentry_IsRejected()
		{
			ExpectErrors();
			var prefab = CreatePrefab<TickBehaviour>();
			var instance = Stage.Spawn(prefab);
			Stage.Tick(0f);

			instance.OnTick = _ => Stage.Tick(0f);
			Stage.Tick(0f);

			Assert.AreEqual(1, Errors);
			Assert.AreEqual(2, instance.Ticks);
			Assert.IsFalse(Stage.IsTicking);
		}

		[Test]
		public void ActorRef_IsNullForDefault_Despawned_AndReused()
		{
			var prefab = CreatePrefab<TickBehaviour>();

			Assert.IsNull(default(ActorRef<TickBehaviour>).Value);
			Assert.IsNull(default(ActorRef).Value);

			var instance = Stage.Spawn(prefab);
			ActorRef<TickBehaviour> typed = instance;
			ActorRef untyped = instance.Actor;
			ActorRef converted = typed;

			Assert.AreSame(instance, typed.Value);
			Assert.AreSame(instance.Actor, untyped.Value);
			Assert.AreSame(instance.Actor, converted.Value);
			Assert.AreEqual(instance.Id, typed.Id);

			instance.Despawn();
			Assert.IsNull(typed.Value, "null as soon as despawn is called");
			Assert.IsNull(untyped.Value);
			Assert.IsFalse(typed.TryGet(out _));

			Stage.Tick(0f);
			var reused = Stage.Spawn(prefab);
			Assert.AreSame(instance, reused, "same object");
			Assert.IsNull(typed.Value, "but the old ref stays null");
			Assert.IsNull(untyped.Value);

			ActorRef<TickBehaviour> fromAcquired = Stage.Acquire(prefab);
			Assert.IsNull(fromAcquired.Value, "a ref to a not-yet-active actor is null");
		}
	}
}
