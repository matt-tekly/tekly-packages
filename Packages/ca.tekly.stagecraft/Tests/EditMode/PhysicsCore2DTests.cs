using System.Collections.Generic;
using NUnit.Framework;
using Tekly.Stagecraft.PhysicsCore2D;
using Unity.U2D.Physics;
using UnityEngine;

namespace Tekly.Stagecraft.Tests
{
	/// <summary>Exercises the 2D adapter against a real, script-stepped PhysicsWorld.</summary>
	public class PhysicsCore2DTests : StageTestBase
	{
		private PhysicsWorld m_world;
		private PhysicsCore2DAdapter m_adapter;
		private int m_worldOwnerKey;

		protected override StageConfig Configure(StageConfig config)
		{
			return config.Register<ActorBody2D>().Register<ContactRecorder>();
		}

		[SetUp]
		public void SetUpPhysics()
		{
			m_world = PhysicsWorld.Create();
			m_worldOwnerKey = m_world.SetOwner(null);
			m_adapter = new PhysicsCore2DAdapter(Stage, PhysicsStepping.Stage, m_world);
		}

		[TearDown]
		public void TearDownPhysics()
		{
			m_adapter.Dispose();
			Stage.Dispose();
			m_world.Destroy(m_worldOwnerKey);
		}

		private ContactRecorder CreateBall(string name)
		{
			var actor = CreatePrefab(name, typeof(ActorBody2D), typeof(ContactRecorder));
			var body = actor.GetComponent<ActorBody2D>();
			body.Shapes = new[] { new ShapeDefinition2D { Kind = ShapeKind2D.Circle, Radius = 0.5f, ContactEvents = true } };
			return actor.GetComponent<ContactRecorder>();
		}

		[Test]
		public void Adapter_SwitchesWorldToScriptStepping_AndAttachesToStage()
		{
			Assert.AreEqual(PhysicsWorld.SimulationType.Script, m_world.simulationType);
			Assert.AreSame(m_adapter, Stage.Physics);
		}

		[Test]
		public void Body_CreatedOnSpawn_WithIdAsUserData_DestroyedAtDespawnSync()
		{
			var ball = Stage.Spawn(CreateBall("ball"), new Pose(new Vector3(1, 2, 0), Quaternion.identity));
			var body = ball.Actor.Get<ActorBody2D>();

			Assert.IsTrue(body.HasBody);
			Assert.AreEqual(ball.Id, ActorId.Unpack(body.Body.userData.int64Value));
			Assert.AreEqual(new Vector2(1, 2), body.Body.position);
			Assert.AreSame(body, m_adapter.Resolve(ball.Id));

			var handle = body.Body;
			ball.Despawn();
			Assert.IsTrue(handle.isValid, "the body lives until the sync point");
			Assert.IsNull(m_adapter.Resolve(ball.Id));

			Stage.Tick(0f);
			Assert.IsFalse(handle.isValid);
			Assert.IsFalse(body.HasBody);
		}

		[Test]
		public void OverlappingBodies_GetContactBegin_DeliveredInFixedTickEnd()
		{
			var a = Stage.Spawn(CreateBall("a"), Pose.identity);
			var b = Stage.Spawn(CreateBall("b"), new Pose(new Vector3(0.25f, 0, 0), Quaternion.identity));

			Stage.FixedTick(0.02f);

			Assert.AreEqual(new[] { b.Id }, a.Begins);
			Assert.AreEqual(new[] { a.Id }, b.Begins);
			Assert.AreEqual(new[] { a.Id }, a.Selves, "Self is the receiving actor");
			Assert.AreEqual(0, m_adapter.PendingEventCount);
		}

		[Test]
		public void ContactWithActorDespawnedByTheOtherSide_ResolvesToNothing()
		{
			var a = Stage.Spawn(CreateBall("a"), Pose.identity);
			var b = Stage.Spawn(CreateBall("b"), new Pose(new Vector3(0.25f, 0, 0), Quaternion.identity));
			a.DespawnOtherOnBegin = true;

			var bId = b.Id;
			Stage.FixedTick(0.02f);

			Assert.AreEqual(new[] { bId }, a.Begins);
			Assert.AreEqual(0, b.Begins.Count, "b was despawned by a's listener, so b's side is never delivered");
			Assert.AreEqual(ActorState.Pooled, b.Actor.State, "and it was pooled in the closing sync");
			Assert.AreEqual(1, Stage.AliveCount);
		}

		[Test]
		public void ReusedInstance_DoesNotReceiveTheOldActorsEvents()
		{
			var prefab = CreateBall("ball");
			var a = Stage.Spawn(prefab, Pose.identity);
			var b = Stage.Spawn(prefab, new Pose(new Vector3(0.25f, 0, 0), Quaternion.identity));
			Stage.FixedTick(0.02f);
			a.Begins.Clear();
			b.Begins.Clear();

			// Move b away so the contact ends, but despawn it before the step: the end event names a dead id
			b.Despawn();
			Stage.Tick(0f);
			var c = Stage.Spawn(prefab, new Pose(new Vector3(10, 0, 0), Quaternion.identity));
			Assert.AreSame(b, c);

			Stage.FixedTick(0.02f);

			Assert.AreEqual(0, c.Begins.Count);
			Assert.AreEqual(0, c.Ends.Count, "the old actor's contact end is not delivered to the new occupant");
		}
	}
}
