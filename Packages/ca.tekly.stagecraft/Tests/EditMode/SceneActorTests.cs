using NUnit.Framework;
using UnityEngine;

namespace Tekly.Stagecraft.Tests
{
	/// <summary>Stage.Adopt and the pool-less lifecycle of an adopted (scene-placed) actor.</summary>
	public class SceneActorTests : StageTestBase
	{
		[Test]
		public void Adopt_InjectsRunsOnCreated_AndLeavesTheActorAcquired()
		{
			var actor = CreatePrefab("placed", typeof(TickBehaviour), typeof(InjectedBehaviour));

			var adopted = Stage.Adopt(actor);

			Assert.AreSame(actor, adopted);
			Assert.AreEqual(ActorState.Acquired, actor.State);
			Assert.AreSame(Stage, actor.Stage);
			Assert.IsFalse(actor.gameObject.activeSelf);
			Assert.IsTrue(actor.Id.IsNone);
			Assert.AreEqual(1, actor.Get<TickBehaviour>().Created);
			Assert.AreEqual(0, actor.Get<TickBehaviour>().Spawned);
			Assert.AreEqual(42, actor.Get<InjectedBehaviour>().Service.Value);
			Assert.AreEqual(1, Stage.AdoptedCount);
			Assert.AreEqual(0, Stage.AliveCount);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void Adopt_OfAnActorThatAlreadyHasAStage_IsAnError()
		{
			var prefab = CreatePrefab<TickBehaviour>();
			var pooled = Stage.Spawn(prefab);
			var placed = Stage.Adopt(CreatePrefab("placed", typeof(TickBehaviour)));

			ExpectErrors();
			Stage.Adopt(pooled.Actor);
			Stage.Adopt(placed);

			Assert.AreEqual(2, Errors);
			Assert.AreEqual(1, Stage.AdoptedCount);
			Assert.AreEqual(1, placed.Get<TickBehaviour>().Created, "OnCreated did not run again");
		}

		[Test]
		public void Adopt_OfAnActiveObject_DeactivatesIt()
		{
			var actor = CreatePrefab("placed", typeof(TickBehaviour));
			actor.gameObject.SetActive(true);

			Stage.Adopt(actor);

			Assert.IsFalse(actor.gameObject.activeSelf);
			Assert.AreEqual(ActorState.Acquired, actor.State);
			Assert.IsFalse(actor.Get<TickBehaviour>().WasActiveInCreated);
			Assert.AreEqual(0, Errors, "a warning, not an error");
		}

		[Test]
		public void Activate_AdoptedActor_IsAliveTickedAndPlaced()
		{
			var actor = Stage.Adopt(CreatePrefab("placed", typeof(TickBehaviour)));
			var behaviour = actor.Get<TickBehaviour>();
			var pose = new Pose(new Vector3(1f, 2f, 3f), Quaternion.Euler(0f, 90f, 0f));

			Stage.Activate(actor, pose, SpawnReason.Placed);

			Assert.IsTrue(actor.IsAlive);
			Assert.IsTrue(actor.gameObject.activeSelf);
			Assert.AreEqual(SpawnReason.Placed, behaviour.LastSpawnReason);
			Assert.AreEqual(1, behaviour.Spawned);
			Assert.AreEqual(pose.position, actor.Transform.position);
			Assert.IsTrue(Stage.TryGet(actor.Id, out Actor found) && found == actor);

			Stage.Tick(0.1f);

			Assert.AreEqual(1, behaviour.Ticks);
			Assert.AreEqual(1, Stage.All<TickBehaviour>().RawCount);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void Despawn_ActiveAdoptedActor_RunsOnDespawnedThenDestroysIt()
		{
			var actor = Stage.Adopt(CreatePrefab("placed", typeof(TickBehaviour)));
			var behaviour = actor.Get<TickBehaviour>();
			Stage.Activate(actor, Pose.identity, SpawnReason.Placed);
			Stage.Tick(0f);

			var despawnedEvents = 0;
			Stage.Despawned.Subscribe(_ => despawnedEvents++);
			ActorRef<TickBehaviour> reference = behaviour;

			actor.Despawn();

			Assert.IsFalse(actor.IsAlive);
			Assert.IsNull(reference.Value);
			Assert.AreEqual(0, Stage.AliveCount);
			Assert.AreEqual(0, behaviour.Despawned, "deferred to the sync");

			Stage.Tick(0f);

			Assert.AreEqual(1, behaviour.Despawned);
			Assert.AreEqual(1, despawnedEvents);
			Assert.IsTrue(actor == null, "destroyed, not pooled");
			Assert.AreEqual(0, Stage.AdoptedCount);
			Assert.AreEqual(0, Stage.RegisteredCount);
			Assert.AreEqual(0, Stage.All<TickBehaviour>().RawCount);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void Despawn_AcquiredAdoptedActor_DestroysItWithoutCallbacks()
		{
			var actor = Stage.Adopt(CreatePrefab("placed", typeof(TickBehaviour)));
			var behaviour = actor.Get<TickBehaviour>();

			Stage.Despawn(actor);

			Assert.IsTrue(actor == null);
			Assert.AreEqual(0, behaviour.Spawned);
			Assert.AreEqual(0, behaviour.Despawned);
			Assert.AreEqual(0, Stage.AdoptedCount);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void Clear_DestroysAdoptedActors_ActiveAndAcquired()
		{
			var active = Stage.Adopt(CreatePrefab("active", typeof(TickBehaviour)));
			var acquired = Stage.Adopt(CreatePrefab("acquired", typeof(TickBehaviour)));
			var activeBehaviour = active.Get<TickBehaviour>();
			Stage.Activate(active, Pose.identity, SpawnReason.Placed);
			Stage.Tick(0f);

			// Pooled actors are unaffected by the adopted ones being around
			var pooled = Stage.Spawn(CreatePrefab<TickBehaviour>());

			Stage.Clear();

			Assert.IsTrue(active == null);
			Assert.IsTrue(acquired == null);
			Assert.AreEqual(1, activeBehaviour.Despawned);
			Assert.AreEqual(ActorState.Pooled, pooled.Actor.State);
			Assert.AreEqual(0, Stage.AdoptedCount);
			Assert.AreEqual(0, Stage.AliveCount);
			Assert.AreEqual(0, Stage.RegisteredCount);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void Dispose_DestroysAdoptedActors()
		{
			var active = Stage.Adopt(CreatePrefab("active", typeof(TickBehaviour)));
			var acquired = Stage.Adopt(CreatePrefab("acquired", typeof(TickBehaviour)));
			Stage.Activate(active, Pose.identity, SpawnReason.Placed);

			Stage.Dispose();

			Assert.IsTrue(active == null);
			Assert.IsTrue(acquired == null);
			Assert.AreEqual(0, Errors);
		}
	}
}
