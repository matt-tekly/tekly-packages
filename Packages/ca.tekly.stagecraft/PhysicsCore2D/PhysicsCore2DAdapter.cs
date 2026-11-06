using System;
using System.Collections.Generic;
using Tekly.Logging;
using Unity.U2D.Physics;

namespace Tekly.Stagecraft.PhysicsCore2D
{
	public enum PhysicsStepping
	{
		/// <summary>The stage steps the world from FixedTick. The world is switched to Script simulation.</summary>
		Stage,

		/// <summary>Something else steps the world (Unity's FixedUpdate mode, or a netcode tick hook) between FixedTickBegin and FixedTickEnd.</summary>
		External
	}

	/// <summary>
	/// Connects a stage to one low-level 2D <see cref="PhysicsWorld"/>. It never puts game code behind
	/// <c>callbackTarget</c>: after every step of its world it copies the world's contact and trigger event spans
	/// into its own buffers, and the stage drains them in FixedTickEnd, where spawning and despawning are safe.
	/// </summary>
	/// <remarks>
	/// How the low-level 2D API behaves (Unity 6000.5, module UnityEngine.PhysicsCore2DModule), from its docs:
	/// <list type="bullet">
	/// <item>World, body and shape are struct handles; <c>isValid</c> says whether the native object still exists.</item>
	/// <item><c>simulationType</c> is FixedUpdate, Update or Script. In Script mode the world only steps when
	/// <c>Simulate(dt)</c> is called; dt of 0 only refreshes events. <c>paused</c> suppresses either kind of step.</item>
	/// <item><c>PhysicsEvents.PreSimulate</c>/<c>PostSimulate</c> are static, main-thread, fire around every step
	/// of every world (manual or automatic) and pass the world. This adapter records on PostSimulate.</item>
	/// <item>The world exposes the last step's events as spans (<c>contactBeginEvents</c>, <c>contactEndEvents</c>,
	/// <c>triggerBeginEvents</c>, <c>triggerEndEvents</c>, <c>contactHitEvents</c>). They are cleared after being
	/// read and invalid once the world changes, so they must be copied immediately.</item>
	/// <item>Contact/trigger callbacks on <c>callbackTarget</c> run on the main thread after the step via
	/// <c>SendContactCallbacks()</c> etc. (automatic when <c>autoContactCallbacks</c>/<c>autoTriggerCallbacks</c>
	/// are on). <c>IContactFilterCallback</c> and <c>IPreSolveCallback</c> run during the step on any thread.</item>
	/// <item>Events need the shape flags: <c>contactEvents</c> on either shape, <c>triggerEvents</c> on both.</item>
	/// <item><c>PhysicsUserData</c> holds int/int64/float/bool/mask/UnityEngine.Object; bodies have a free
	/// <c>userData</c> and an owner-keyed <c>ownerUserData</c>. The owning <see cref="ActorId"/> goes in
	/// <c>userData.int64Value</c>.</item>
	/// <item>With <see cref="PhysicsStepping.External"/> and the world in FixedUpdate mode, Unity's step and
	/// the stage's FixedTick both run from FixedUpdate in script-execution order, so events can arrive one
	/// fixed step late. Stage-owned stepping has no such gap and is the default.</item>
	/// </list>
	/// </remarks>
	public sealed class PhysicsCore2DAdapter : IStagePhysics, IDisposable
	{
		public Stage Stage { get; }
		public PhysicsWorld World => m_world;
		public PhysicsStepping Stepping { get; }

		/// <summary>Events waiting for delivery; for diagnostics.</summary>
		public int PendingEventCount => m_contactBegin.Count + m_contactEnd.Count + m_triggerBegin.Count + m_triggerEnd.Count;

		private readonly PhysicsWorld m_world;
		private readonly PhysicsWorld.SimulationType m_previousSimulationType;
		private readonly TkLogger m_logger = TkLogger.Get<PhysicsCore2DAdapter>();

		private readonly List<PairEvent> m_contactBegin = new List<PairEvent>(256);
		private readonly List<PairEvent> m_contactEnd = new List<PairEvent>(256);
		private readonly List<PairEvent> m_triggerBegin = new List<PairEvent>(256);
		private readonly List<PairEvent> m_triggerEnd = new List<PairEvent>(256);

		private ActorBody2D[] m_bodies = new ActorBody2D[256];
		private bool m_disposed;

		private struct PairEvent
		{
			public ActorId A;
			public ActorId B;
			public PhysicsShape ShapeA;
			public PhysicsShape ShapeB;
		}
		
		public PhysicsCore2DAdapter(Stage stage, PhysicsStepping stepping = PhysicsStepping.Stage, PhysicsWorld world = default)
		{
			Stage = stage;
			Stepping = stepping;
			m_world = world.isValid ? world : PhysicsWorld.defaultWorld;

			m_previousSimulationType = m_world.simulationType;
			if (stepping == PhysicsStepping.Stage) {
				m_world.simulationType = PhysicsWorld.SimulationType.Script;
			}

			PhysicsEvents.PostSimulate += OnPostSimulate;
			stage.Physics = this;
		}

		public void Step(float fixedDeltaTime)
		{
			if (Stepping == PhysicsStepping.Stage && m_world.isValid) {
				m_world.Simulate(fixedDeltaTime);
			}
		}

		public void DeliverEvents()
		{
			Deliver(m_contactBegin, EventKind.ContactBegin);
			Deliver(m_contactEnd, EventKind.ContactEnd);
			Deliver(m_triggerBegin, EventKind.TriggerBegin);
			Deliver(m_triggerEnd, EventKind.TriggerEnd);
		}

		public void Dispose()
		{
			if (m_disposed) {
				return;
			}

			m_disposed = true;
			PhysicsEvents.PostSimulate -= OnPostSimulate;

			if (Stepping == PhysicsStepping.Stage && m_world.isValid) {
				m_world.simulationType = m_previousSimulationType;
			}

			if (ReferenceEquals(Stage.Physics, this)) {
				Stage.Physics = null;
			}

			m_contactBegin.Clear();
			m_contactEnd.Clear();
			m_triggerBegin.Clear();
			m_triggerEnd.Clear();
		}

		// ----- Bodies ---------------------------------------------------------------------------------------------

		internal void Register(ActorBody2D body)
		{
			var index = body.Actor.Id.Index;
			if (index >= m_bodies.Length) {
				var size = m_bodies.Length;
				while (size <= index) {
					size *= 2;
				}

				Array.Resize(ref m_bodies, size);
			}

			m_bodies[index] = body;
		}

		internal void Unregister(ActorBody2D body)
		{
			var index = body.Actor.Id.Index;
			if (index < m_bodies.Length && ReferenceEquals(m_bodies[index], body)) {
				m_bodies[index] = null;
			}
		}

		/// <summary>The live body for an id, or null if the actor is gone or has no body.</summary>
		public ActorBody2D Resolve(ActorId id)
		{
			if (id.IsNone || id.Index >= m_bodies.Length) {
				return null;
			}

			var body = m_bodies[id.Index];
			if (ReferenceEquals(body, null)) {
				return null;
			}

			var actor = body.Actor;
			return actor.State == ActorState.Active && actor.Id == id ? body : null;
		}

		// ----- Recording ------------------------------------------------------------------------------------------

		private void OnPostSimulate(PhysicsWorld world, float deltaTime)
		{
			if (!world.Equals(m_world)) {
				return;
			}

			// The spans are only valid until the world changes and are cleared after this read, so copy now.
			// Only the ids and shape handles are kept; nothing from here reaches game code until DeliverEvents.
			foreach (var evt in world.contactBeginEvents) {
				Record(m_contactBegin, evt.shapeA, evt.shapeB);
			}

			foreach (var evt in world.contactEndEvents) {
				Record(m_contactEnd, evt.shapeA, evt.shapeB);
			}

			foreach (var evt in world.triggerBeginEvents) {
				Record(m_triggerBegin, evt.triggerShape, evt.visitorShape);
			}

			foreach (var evt in world.triggerEndEvents) {
				Record(m_triggerEnd, evt.triggerShape, evt.visitorShape);
			}
		}

		private static void Record(List<PairEvent> into, PhysicsShape shapeA, PhysicsShape shapeB)
		{
			var a = ResolveId(shapeA);
			var b = ResolveId(shapeB);

			if (a.IsNone && b.IsNone) {
				return;
			}

			into.Add(new PairEvent { A = a, B = b, ShapeA = shapeA, ShapeB = shapeB });
		}

		/// <summary>The owning actor from the body's user data; None for shapes that are gone or belong to no actor.</summary>
		public static ActorId ResolveId(PhysicsShape shape)
		{
			if (!shape.isValid) {
				return ActorId.None;
			}

			var body = shape.body;
			if (!body.isValid) {
				return ActorId.None;
			}

			return ActorId.Unpack(body.userData.int64Value);
		}

		// ----- Delivery -------------------------------------------------------------------------------------------

		private enum EventKind
		{
			ContactBegin,
			ContactEnd,
			TriggerBegin,
			TriggerEnd
		}

		private void Deliver(List<PairEvent> events, EventKind kind)
		{
			// Index loop: listeners may step the world? No, but they may despawn, which doesn't touch this list.
			for (var i = 0; i < events.Count; i++) {
				var evt = events[i];

				var bodyA = Resolve(evt.A);
				var bodyB = Resolve(evt.B);

				if (!ReferenceEquals(bodyA, null)) {
					bodyA.Deliver(kind == EventKind.ContactBegin || kind == EventKind.ContactEnd, kind == EventKind.ContactBegin || kind == EventKind.TriggerBegin,
						new Contact2D(evt.A, evt.B, bodyB, evt.ShapeA, evt.ShapeB));
				}

				// Re-resolve: A's listener may have despawned B
				bodyB = Resolve(evt.B);
				if (!ReferenceEquals(bodyB, null)) {
					bodyA = Resolve(evt.A);
					bodyB.Deliver(kind == EventKind.ContactBegin || kind == EventKind.ContactEnd, kind == EventKind.ContactBegin || kind == EventKind.TriggerBegin,
						new Contact2D(evt.B, evt.A, bodyA, evt.ShapeB, evt.ShapeA));
				}
			}

			events.Clear();
		}
	}
}
