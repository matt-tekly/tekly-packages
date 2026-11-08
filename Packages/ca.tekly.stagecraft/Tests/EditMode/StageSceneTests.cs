using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Tekly.Stagecraft.Tests
{
	/// <summary>
	/// StageScene: adopting and activating a baked list, and the bake itself. The test runner's scene is empty apart
	/// from what the tests create, so the bake sees exactly that.
	/// </summary>
	public class StageSceneTests : StageTestBase
	{
		private Scene m_scene;
		private GameObject m_stageObject;
		private readonly List<GameObject> m_extras = new List<GameObject>();

		[SetUp]
		public void SetUpScene()
		{
			m_scene = SceneManager.GetActiveScene();
			m_stageObject = Extra(new GameObject("StageScene"));
		}

		[TearDown]
		public void TearDownScene()
		{
			// Edit mode sends no OnDestroy, so the stage lets go of the actors before anything destroys them
			Stage.Dispose();

			foreach (var extra in m_extras) {
				if (extra != null) {
					Object.DestroyImmediate(extra);
				}
			}

			m_extras.Clear();
		}

		private GameObject Extra(GameObject go)
		{
			m_extras.Add(go);
			return go;
		}

		private StageScene CreateStageScene(params Actor[] actors)
		{
			var stageScene = m_stageObject.AddComponent<StageScene>();
			var entries = new SceneActorEntry[actors.Length];
			for (var i = 0; i < actors.Length; i++) {
				entries[i] = new SceneActorEntry(actors[i], SceneActorId.New());
			}

			stageScene.SetEntries(entries);
			return stageScene;
		}

		private Actor CreatePlaced(string name, Vector3 position)
		{
			var actor = CreatePrefab(name, typeof(TickBehaviour));
			actor.transform.position = position;
			return actor;
		}

		[Test]
		public void AdoptThenActivate_BringsEveryBakedActorAliveAtItsPlacedPose()
		{
			var a = CreatePlaced("a", new Vector3(1f, 0f, 0f));
			var b = CreatePlaced("b", new Vector3(2f, 0f, 0f));
			var stageScene = CreateStageScene(a, b);

			stageScene.Adopt(Stage);

			Assert.AreSame(Stage, stageScene.Stage);
			Assert.AreEqual(ActorState.Acquired, a.State);
			Assert.AreEqual(ActorState.Acquired, b.State);
			Assert.AreEqual(2, Stage.AdoptedCount);

			var activated = stageScene.Activate();

			Assert.AreEqual(2, activated);
			Assert.AreEqual(2, Stage.AliveCount);
			Assert.IsTrue(a.IsAlive && b.IsAlive);
			Assert.AreEqual(new Vector3(1f, 0f, 0f), a.Transform.position);
			Assert.AreEqual(new Vector3(2f, 0f, 0f), b.Transform.position);
			Assert.AreEqual(SpawnReason.Placed, a.Get<TickBehaviour>().LastSpawnReason);

			Stage.Tick(0f);
			Assert.AreEqual(2, Stage.All<TickBehaviour>().RawCount);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void Activate_SkipsActorsAlreadyActivatedOrDespawned_AndIsIdempotent()
		{
			var a = CreatePlaced("a", Vector3.zero);
			var b = CreatePlaced("b", Vector3.zero);
			var c = CreatePlaced("c", Vector3.zero);
			var stageScene = CreateStageScene(a, b, c);
			stageScene.Adopt(Stage);

			// A restore: one activated by hand as Restored, one dead
			Stage.Activate(a, Pose.identity, SpawnReason.Restored);
			Stage.Despawn(b);

			Assert.AreEqual(1, stageScene.Activate());
			Assert.AreEqual(SpawnReason.Restored, a.Get<TickBehaviour>().LastSpawnReason);
			Assert.AreEqual(SpawnReason.Placed, c.Get<TickBehaviour>().LastSpawnReason);
			Assert.IsTrue(b == null);
			Assert.AreEqual(2, Stage.AliveCount);

			Assert.AreEqual(0, stageScene.Activate(), "nothing left to activate");
			Assert.AreEqual(1, a.Get<TickBehaviour>().Spawned);
			Assert.AreEqual(0, Errors);
		}

		[Test]
		public void TryGet_FindsAdoptedActorsById_UntilTheyAreDestroyed()
		{
			var a = CreatePlaced("a", Vector3.zero);
			var b = CreatePlaced("b", Vector3.zero);
			var stageScene = CreateStageScene(a, b);
			var idA = stageScene.Entries[0].Id;
			var idB = stageScene.Entries[1].Id;

			Assert.IsFalse(stageScene.TryGet(idA, out _), "nothing before Adopt");

			stageScene.Adopt(Stage);
			stageScene.Activate();

			Assert.IsTrue(stageScene.TryGet(idA, out var foundA) && foundA == a);
			Assert.IsTrue(stageScene.TryGet(idB, out var foundB) && foundB == b);
			Assert.IsFalse(stageScene.TryGet(SceneActorId.New(), out _));

			b.Despawn();
			Stage.Tick(0f);

			Assert.IsFalse(stageScene.TryGet(idB, out _));
			Assert.IsTrue(stageScene.TryGet(idA, out _));
		}

		[Test]
		public void Adopt_SkipsMissingEntries_AndTwiceIsAnError()
		{
			var a = CreatePlaced("a", Vector3.zero);
			var stageScene = CreateStageScene(a, null);

			stageScene.Adopt(Stage);

			Assert.AreEqual(1, Stage.AdoptedCount);
			Assert.AreEqual(0, Errors, "a missing entry is a warning");

			ExpectErrors();
			stageScene.Adopt(Stage);
			Assert.AreEqual(1, Errors);

			Assert.Throws<ArgumentNullException>(() => stageScene.Adopt(null));
		}

		[Test]
		public void Activate_BeforeAdopt_IsAnError()
		{
			var stageScene = CreateStageScene(CreatePlaced("a", Vector3.zero));

			ExpectErrors();
			Assert.AreEqual(0, stageScene.Activate());
			Assert.AreEqual(1, Errors);
		}

		[Test]
		public void Bake_CollectsActorsInHierarchyOrder_KeepsIdsOfKnownActors_SkipsNestedActors()
		{
			var a = CreatePlaced("a", Vector3.zero);
			var b = CreatePlaced("b", Vector3.zero);
			var c = CreatePlaced("c", Vector3.zero);

			// A nested actor: its behaviours are not the parent's, but it is not a scene actor of its own either
			var nested = Extra(new GameObject("nested", typeof(Actor), typeof(PlainBehaviour)));
			nested.transform.SetParent(b.transform, false);
			nested.GetComponent<Actor>().Collect();

			var stageScene = CreateStageScene(a, b);
			var idA = stageScene.Entries[0].Id;
			var idB = stageScene.Entries[1].Id;

			stageScene.Bake(new SceneBake(m_scene));

			var entries = stageScene.Entries;
			Assert.AreEqual(3, entries.Count);
			Assert.AreSame(a, entries[0].Actor);
			Assert.AreSame(b, entries[1].Actor);
			Assert.AreSame(c, entries[2].Actor);
			Assert.AreEqual(idA, entries[0].Id, "known actors keep their ids");
			Assert.AreEqual(idB, entries[1].Id);
			Assert.IsFalse(entries[2].Id.IsNone, "new actors get one");
			Assert.AreNotEqual(idA, entries[2].Id);
			Assert.AreNotEqual(idB, entries[2].Id);
			Assert.AreEqual(0, Errors);

			// Rebaking changes nothing
			stageScene.Bake(new SceneBake(m_scene));
			Assert.AreEqual(3, stageScene.Entries.Count);
			Assert.AreEqual(idA, stageScene.Entries[0].Id);
			Assert.AreEqual(entries[2].Id, stageScene.Entries[2].Id);
		}

		[Test]
		public void Bake_FindsInactiveActors()
		{
			var a = CreatePlaced("a", Vector3.zero);
			var holder = Extra(new GameObject("holder"));
			holder.SetActive(false);
			a.transform.SetParent(holder.transform, false);

			var stageScene = CreateStageScene();
			stageScene.Bake(new SceneBake(m_scene));

			Assert.AreEqual(1, stageScene.Entries.Count);
			Assert.AreSame(a, stageScene.Entries[0].Actor);
		}

		[Test]
		public void Bake_WithTwoStageScenes_IsAnError()
		{
			var stageScene = CreateStageScene();
			Extra(new GameObject("other", typeof(StageScene)));

			ExpectErrors();
			stageScene.Bake(new SceneBake(m_scene));

			Assert.AreEqual(1, Errors);
		}

		[Test]
		public void SceneActorId_New_IsNeverNone_AndRoundTripsThroughEquality()
		{
			var id = SceneActorId.New();

			Assert.IsFalse(id.IsNone);
			Assert.IsTrue(SceneActorId.None.IsNone);
			Assert.AreEqual(id, new SceneActorId(id.Value));
			Assert.AreNotEqual(id, SceneActorId.New());
			Assert.AreEqual(16, id.ToString().Length);
		}
	}
}
