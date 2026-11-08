using System.Collections;
using NUnit.Framework;
using Tekly.Stagecraft.Tests;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tekly.Stagecraft.PlayModeTests
{
	/// <summary>Needs Awake/OnEnable/OnDestroy, which edit mode doesn't send to ordinary MonoBehaviours.</summary>
	public class StageScenePlayModeTests
	{
		private Stage m_stage;
		private GameObject m_root;

		[SetUp]
		public void SetUp()
		{
			m_stage = new Stage(new StageConfig().Register<LifeCounter>());

			// Stands in for a scene about to load: nothing in it has had Awake yet
			m_root = new GameObject("scene");
			m_root.SetActive(false);
		}

		[TearDown]
		public void TearDown()
		{
			m_stage.Dispose();
			Object.Destroy(m_root);
		}

		private LifeCounter CreatePlaced(string name)
		{
			var go = new GameObject(name);
			go.transform.SetParent(m_root.transform, false);
			go.AddComponent<Actor>();
			var counter = go.AddComponent<LifeCounter>();
			go.GetComponent<Actor>().Collect();
			return counter;
		}

		private StageScene CreateStageScene(params LifeCounter[] placed)
		{
			var go = new GameObject("StageScene");
			go.transform.SetParent(m_root.transform, false);
			var stageScene = go.AddComponent<StageScene>();

			var entries = new SceneActorEntry[placed.Length];
			for (var i = 0; i < placed.Length; i++) {
				entries[i] = new SceneActorEntry(placed[i].GetComponent<Actor>(), SceneActorId.New());
			}

			stageScene.SetEntries(entries);
			return stageScene;
		}

		[Test]
		public void StageScene_PutsBakedActorsToSleep_BeforeTheirAwake()
		{
			var a = CreatePlaced("a");
			var b = CreatePlaced("b");
			var stageScene = CreateStageScene(a, b);

			// The "scene load": every Awake in execution order, StageScene's first
			m_root.SetActive(true);

			Assert.AreEqual(0, a.Awakes, "deactivated before its Awake");
			Assert.AreEqual(0, a.Enables);
			Assert.AreEqual(0, b.Awakes);
			Assert.IsFalse(a.gameObject.activeSelf);
			Assert.IsFalse(b.gameObject.activeSelf);

			stageScene.Adopt(m_stage);
			Assert.AreEqual(0, a.Awakes, "still asleep while Acquired");

			stageScene.Activate();

			Assert.AreEqual(1, a.Awakes);
			Assert.AreEqual(1, a.Enables);
			Assert.AreEqual(1, a.Spawns);
			Assert.AreEqual(SpawnReason.Placed, a.SpawnReason);
			Assert.IsTrue(a.IsAlive && b.IsAlive);
			Assert.AreEqual(2, m_stage.AliveCount);
		}

		[UnityTest]
		public IEnumerator ExternalDestroy_OfAdoptedActor_IsForgottenAtNextSync()
		{
			var a = CreatePlaced("a");
			var b = CreatePlaced("b");
			var stageScene = CreateStageScene(a, b);
			m_root.SetActive(true);
			stageScene.Adopt(m_stage);
			stageScene.Activate();
			m_stage.Tick(0f);

			var id = a.Id;
			Object.Destroy(a.gameObject);

			yield return null; // OnDestroy runs at the end of the frame

			Assert.AreEqual(ActorState.Destroyed, a.Actor.State);
			Assert.IsFalse(m_stage.TryGet(id, out _));
			Assert.AreEqual(1, m_stage.AliveCount);

			m_stage.Tick(0f);

			Assert.AreEqual(0, a.Despawns, "no OnDespawned on a destroyed object");
			Assert.AreEqual(1, m_stage.AdoptedCount);
			Assert.AreEqual(1, m_stage.RegisteredCount);
			Assert.IsTrue(b.IsAlive);
		}

		[UnityTest]
		public IEnumerator Despawn_OfAdoptedActor_DestroysTheObject()
		{
			var a = CreatePlaced("a");
			var stageScene = CreateStageScene(a);
			m_root.SetActive(true);
			stageScene.Adopt(m_stage);
			stageScene.Activate();
			m_stage.Tick(0f);

			a.Despawn();
			m_stage.Tick(0f);

			Assert.AreEqual(1, a.Despawns);
			Assert.AreEqual(0, m_stage.AdoptedCount);

			yield return null; // Destroy lands at the end of the frame

			Assert.IsTrue(a == null);
			Assert.AreEqual(0, m_stage.RegisteredCount);
		}
	}
}
