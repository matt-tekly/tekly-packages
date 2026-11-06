namespace Tekly.Stagecraft
{
	/// <summary>
	/// Batch logic for every live T, registered with StageConfig.RegisterSystem. This is how hot paths are written:
	/// one call per tick with the whole list, instead of one virtual call per actor. Created through the stage's
	/// injector, so [Inject] fields work; keep a default constructor.
	/// </summary>
	public abstract class ActorSystem<T> where T : ActorBehaviour
	{
		public Stage Stage { get; internal set; }

		/// <summary>The behaviour joined the list, at the first sync after its spawn.</summary>
		public virtual void OnAdded(T behaviour) { }

		/// <summary>The behaviour is leaving the list, at the sync after its despawn, before its OnDespawned. Its actor is not alive.</summary>
		public virtual void OnRemoved(T behaviour) { }

		/// <summary>Presentation, from Stage.Tick. Runs after the per-object ticks of the same group.</summary>
		public virtual void Tick(ActorList<T> behaviours, float deltaTime) { }

		/// <summary>Gameplay, from Stage.FixedTick. Runs after the per-object fixed ticks of the same group.</summary>
		public virtual void FixedTick(ActorList<T> behaviours, float fixedDeltaTime) { }
	}
}
