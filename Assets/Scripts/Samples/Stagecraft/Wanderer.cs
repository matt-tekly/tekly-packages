using Tekly.Stagecraft;
using Tekly.Stagecraft.PhysicsCore2D;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TeklySample.Samples.Stagecraft
{
	public struct WandererArgs
	{
		public float Speed;
		public float Lifetime;
	}

	/// <summary>
	/// Random-walks for a while, then despawns. Ticked per object:
	/// FixedTick steers the body and checks the lifetime
	/// Tick fades the sprite. Fine for a handful of actors; for a crowd, see <see cref="Drifter"/>.
	/// </summary>
	public class Wanderer : ActorBehaviour, ISpawnArgs<WandererArgs>
	{
		private WandererArgs m_args;
		private float m_dies;
		private float m_turnAt;
		private Vector2 m_heading;
		private ActorBody2D m_body;
		private SpriteRenderer m_renderer;
		private Color m_colour;

		public void Initialize(in WandererArgs args)
		{
			m_args = args;
		}

		public override void OnCreated()
		{
			m_body = Actor.Get<ActorBody2D>();
			m_renderer = GetComponent<SpriteRenderer>();
			m_colour = m_renderer.color;
		}

		public override void OnSpawned()
		{
			m_dies = Stage.Time.FixedElapsed + m_args.Lifetime;
			m_turnAt = 0f;
		}

		public override void FixedTick(float fixedDeltaTime)
		{
			var now = Stage.Time.FixedElapsed;
			if (now >= m_dies) {
				Despawn();
				return;
			}

			if (now >= m_turnAt) {
				m_turnAt = now + Random.Range(0.3f, 1.2f);
				m_heading = Random.insideUnitCircle.normalized;
			}

			// PhysicsBody is a struct handle, so the property can't be set through m_body.Body directly
			var body = m_body.Body;
			body.linearVelocity = m_heading * m_args.Speed;
		}

		public override void Tick(float deltaTime)
		{
			var remaining = Mathf.Clamp01((m_dies - Stage.Time.FixedElapsed) / m_args.Lifetime);
			m_renderer.color = m_colour * (0.4f + 0.6f * remaining);
		}
	}
}
