using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Tekly.Stagecraft.Tests;
using UnityEngine.TestTools;

namespace Tekly.Stagecraft.PlayModeTests
{
	/// <summary>Needs Awake/OnEnable/OnDestroy, which edit mode doesn't send to ordinary MonoBehaviours.</summary>
	public class StagePlayModeTests
	{
		private Stage m_stage;
		private GameObject m_prefab;

		[SetUp]
		public void SetUp()
		{
			m_stage = new Stage(new StageConfig().Register<LifeCounter>());

			m_prefab = new GameObject("prefab");
			m_prefab.SetActive(false);
			m_prefab.AddComponent<Actor>();
			m_prefab.AddComponent<LifeCounter>();
		}

		[TearDown]
		public void TearDown()
		{
			m_stage.Dispose();
			Object.Destroy(m_prefab);
		}

		[Test]
		public void PooledInstance_GetsNoAwakeOrEnable_UntilSpawned()
		{
			var prefab = m_prefab.GetComponent<LifeCounter>();

			var acquired = m_stage.Acquire(prefab);
			Assert.AreEqual(0, acquired.Awakes);
			Assert.AreEqual(0, acquired.Enables);

			m_stage.Activate(acquired.Actor, Pose.identity);
			Assert.AreEqual(1, acquired.Awakes);
			Assert.AreEqual(1, acquired.Enables);
			Assert.AreEqual(1, acquired.Spawns);

			acquired.Despawn();
			m_stage.Tick(0f);
			var again = m_stage.Spawn(prefab);

			Assert.AreSame(acquired, again);
			Assert.AreEqual(1, again.Awakes, "Awake once per instance");
			Assert.AreEqual(2, again.Enables, "OnEnable once per spawn");
			Assert.AreEqual(2, again.Spawns);
		}

		[UnityTest]
		public IEnumerator ExternalDestroy_OfActiveActor_IsCleanedUpAtNextSync()
		{
			var prefab = m_prefab.GetComponent<LifeCounter>();
			var instance = m_stage.Spawn(prefab);
			var other = m_stage.Spawn(prefab);
			m_stage.Tick(0f);

			var id = instance.Id;
			ActorRef<LifeCounter> reference = instance;
			Object.Destroy(instance.gameObject);

			yield return null; // OnDestroy runs at the end of the frame

			Assert.AreEqual(ActorState.Destroyed, instance.Actor.State);
			Assert.IsNull(reference.Value, "no Unity null check needed");
			Assert.IsFalse(m_stage.TryGet(id, out _));
			Assert.AreEqual(1, m_stage.AliveCount);

			m_stage.Tick(0f);

			Assert.AreEqual(0, instance.Despawns, "no OnDespawned on a destroyed object");
			Assert.AreEqual(1, m_stage.RegisteredCount);
			Assert.AreEqual(1, m_stage.All<LifeCounter>().RawCount);
			Assert.IsTrue(other.IsAlive);

			// The pool no longer hands the destroyed instance out
			var fresh = m_stage.Spawn(prefab);
			Assert.AreNotSame(instance, fresh);
			Assert.IsTrue(fresh.IsAlive);
		}

		[UnityTest]
		public IEnumerator ExternalDestroy_OfPooledInstance_IsSkippedByThePool()
		{
			var prefab = m_prefab.GetComponent<LifeCounter>();
			var instance = m_stage.Spawn(prefab);
			instance.Despawn();
			m_stage.Tick(0f);

			Object.Destroy(instance.gameObject);
			yield return null;

			var fresh = m_stage.Spawn(prefab);
			Assert.AreNotSame(instance, fresh);
			Assert.IsTrue(fresh.IsAlive);
		}

		[UnityTest]
		public IEnumerator Dispose_DestroysEverything()
		{
			var prefab = m_prefab.GetComponent<LifeCounter>();
			var instance = m_stage.Spawn(prefab);
			var root = m_stage.Root;

			m_stage.Dispose();
			yield return null;

			Assert.AreEqual(1, instance.Despawns);
			Assert.IsTrue(root == null);
			Assert.IsTrue(instance == null);
		}

		[UnityTest]
		public IEnumerator StageRunner_TicksFromUpdateAndFixedUpdate()
		{
			var prefab = m_prefab.GetComponent<LifeCounter>();
			m_stage.Spawn(prefab);
			var runner = StageRunner.Create(m_stage);

			yield return new WaitForSeconds(0.1f);

			Assert.Greater(m_stage.Time.Elapsed, 0f);
			Assert.Greater(m_stage.Time.FixedStep, 0);

			Object.Destroy(runner.gameObject);
		}
	}
}
