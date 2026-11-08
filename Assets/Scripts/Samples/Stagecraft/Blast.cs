using Tekly.Injectors;
using Tekly.Stagecraft;
using Tekly.Stagecraft.PhysicsCore2D;

namespace TeklySample.Samples.Stagecraft
{
	public struct BlastArgs
	{
		public float Lifetime;
	}

	/// <summary>
	/// A short-lived trigger. Whatever it overlaps is despawned, from the buffered trigger event the stage delivers
	/// after the physics step, never from inside the physics system. A mass despawn in one fixed step is what
	/// the sync point and swap-remove lists are for. The prefab's shape is in its own contact category and excludes
	/// it from its contacts, so two blasts never pair in the physics step at all.
	/// </summary>
	public class Blast : ActorBehaviour, ISpawnArgs<BlastArgs>, ITriggerListener2D
	{
		// Injected once, when the pooled instance is created; the same service for every spawn of this instance
		[Inject] private SampleStats m_stats;

		private float m_dies;

		public void Initialize(in BlastArgs args)
		{
			m_dies = Stage.Time.FixedElapsed + args.Lifetime;
		}

		public override void OnSpawned()
		{
			m_stats.Blasts++;
		}

		public override void FixedTick(float fixedDeltaTime)
		{
			if (Stage.Time.FixedElapsed >= m_dies) {
				Despawn();
			}
		}

		public void OnTriggerBegin(in Contact2D contact)
		{
			// OtherBody is null when the other actor is already gone (despawned earlier this delivery, or reused)
			if (contact.OtherBody != null) {
				contact.OtherBody.Despawn();
				m_stats.Kills++;
			}
		}

		public void OnTriggerEnd(in Contact2D contact) { }
	}
}
