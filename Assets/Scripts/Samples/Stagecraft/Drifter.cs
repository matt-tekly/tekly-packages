using System;
using Tekly.Stagecraft;
using Tekly.Stagecraft.PhysicsCore2D;
using Unity.U2D.Physics;
using UnityEngine;

namespace TeklySample.Samples.Stagecraft
{
	public struct DrifterArgs
	{
		public Vector2 Target;
		public float Speed;
		public float Lifetime;
	}

	/// <summary>
	/// Heads for a point, then despawns. It overrides neither Tick nor FixedTick, so the stage never calls it per
	/// object; <see cref="DriftSystem"/> moves every live drifter in one FixedTick. Crowd behaviour (enemy
	/// movement, pickup attraction) belongs in a system like this rather than in per-object ticks.
	/// </summary>
	public class Drifter : ActorBehaviour, ISpawnArgs<DrifterArgs>
	{
		public Vector2 Target;
		public float Speed;
		public float Dies;
		public ActorBody2D Body;

		public void Initialize(in DrifterArgs args)
		{
			Target = args.Target;
			Speed = args.Speed;
			Dies = Stage.Time.FixedElapsed + args.Lifetime;
		}

		public override void OnCreated()
		{
			Body = Actor.Get<ActorBody2D>();
		}
	}

	public class DriftSystem : ActorSystem<Drifter>
	{
		// Reused every step: the whole crowd's velocities go to the physics world in one SetBatchVelocity call
		// instead of one interop call per body. That is what a system can do that per-object ticks can't; the
		// per-body position read below is still an interop call each and now dominates this system's cost
		private PhysicsBody.BatchVelocity[] m_velocities = new PhysicsBody.BatchVelocity[1024];

		public override void FixedTick(ActorList<Drifter> drifters, float fixedDeltaTime)
		{
			var now = Stage.Time.FixedElapsed;
			var count = 0;

			if (m_velocities.Length < drifters.RawCount) {
				m_velocities = new PhysicsBody.BatchVelocity[Mathf.NextPowerOfTwo(drifters.RawCount)];
			}

			foreach (var drifter in drifters) {
				var body = drifter.Body.Body;
				var toTarget = drifter.Target - body.position;

				// Arrived, or boxed in by the crowd for too long. Despawning mid-iteration is fine: the actor is
				// skipped by the rest of this loop and removed from the list at the next sync point
				if (toTarget.sqrMagnitude < 1f || now >= drifter.Dies) {
					drifter.Despawn();
					continue;
				}

				m_velocities[count++] = new PhysicsBody.BatchVelocity(body) { linearVelocity = toTarget.normalized * drifter.Speed };
			}

			// The world warns on an empty batch
			if (count > 0) {
				PhysicsBody.SetBatchVelocity(new ReadOnlySpan<PhysicsBody.BatchVelocity>(m_velocities, 0, count));
			}
		}
	}
}
