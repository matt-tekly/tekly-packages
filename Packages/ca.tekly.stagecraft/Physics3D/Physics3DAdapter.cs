using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Stagecraft.Physics3D
{
	/// <summary>One side of a recorded 3D contact. <see cref="OtherBody"/> is null if that actor is no longer alive.</summary>
	public readonly struct Contact3D
	{
		public readonly ActorId Self;
		public readonly ActorId Other;
		public readonly ActorBody3D OtherBody;

		public Contact3D(ActorId self, ActorId other, ActorBody3D otherBody)
		{
			Self = self;
			Other = other;
			OtherBody = otherBody;
		}
	}

	public interface IContactListener3D
	{
		void OnContactBegin(in Contact3D contact);
		void OnContactEnd(in Contact3D contact);
	}

	public interface ITriggerListener3D
	{
		void OnTriggerBegin(in Contact3D contact);
		void OnTriggerEnd(in Contact3D contact);
	}

	public enum PhysicsStepping3D
	{
		Stage,
		External
	}

	/// <summary>
	/// Stub of the PhysX adapter, enough to show the core's contract holds for a second backend: PhysX's
	/// MonoBehaviour messages only record (id, id) pairs here, and the stage delivers them in FixedTickEnd.
	/// With <see cref="PhysicsStepping3D.Stage"/> it puts Physics in script mode and steps it from FixedTick.
	/// </summary>
	public sealed class Physics3DAdapter : IStagePhysics, IDisposable
	{
		public Stage Stage { get; }
		public PhysicsStepping3D Stepping { get; }

		private readonly SimulationMode m_previousMode;
		private readonly List<(ActorId a, ActorId b, bool trigger, bool begin)> m_events = new List<(ActorId, ActorId, bool, bool)>(256);
		private ActorBody3D[] m_bodies = new ActorBody3D[256];

		public Physics3DAdapter(Stage stage, PhysicsStepping3D stepping = PhysicsStepping3D.Stage)
		{
			Stage = stage;
			Stepping = stepping;

			m_previousMode = Physics.simulationMode;
			if (stepping == PhysicsStepping3D.Stage) {
				Physics.simulationMode = SimulationMode.Script;
			}

			stage.Physics = this;
		}

		public void Step(float fixedDeltaTime)
		{
			if (Stepping == PhysicsStepping3D.Stage) {
				Physics.Simulate(fixedDeltaTime);
			}
		}

		public void DeliverEvents()
		{
			for (var i = 0; i < m_events.Count; i++) {
				var (a, b, trigger, begin) = m_events[i];

				var bodyA = Resolve(a);
				if (!ReferenceEquals(bodyA, null)) {
					bodyA.Deliver(trigger, begin, new Contact3D(a, b, Resolve(b)));
				}
			}

			m_events.Clear();
		}

		public void Dispose()
		{
			if (Stepping == PhysicsStepping3D.Stage) {
				Physics.simulationMode = m_previousMode;
			}

			if (ReferenceEquals(Stage.Physics, this)) {
				Stage.Physics = null;
			}
		}

		/// <summary>Called from PhysX messages: records only.</summary>
		internal void Record(ActorId self, ActorId other, bool trigger, bool begin)
		{
			m_events.Add((self, other, trigger, begin));
		}

		internal void Register(ActorBody3D body)
		{
			var index = body.Actor.Id.Index;
			if (index >= m_bodies.Length) {
				Array.Resize(ref m_bodies, Math.Max(index + 1, m_bodies.Length * 2));
			}

			m_bodies[index] = body;
		}

		internal void Unregister(ActorBody3D body)
		{
			var index = body.Actor.Id.Index;
			if (index < m_bodies.Length && ReferenceEquals(m_bodies[index], body)) {
				m_bodies[index] = null;
			}
		}

		public ActorBody3D Resolve(ActorId id)
		{
			if (id.IsNone || id.Index >= m_bodies.Length) {
				return null;
			}

			var body = m_bodies[id.Index];
			if (ReferenceEquals(body, null)) {
				return null;
			}

			return body.Actor.State == ActorState.Active && body.Actor.Id == id ? body : null;
		}
	}
}
