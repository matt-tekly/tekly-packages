namespace Tekly.Stagecraft
{
	/// <summary>
	/// The only thing the core knows about physics. An adapter assembly implements it per backend. Within a fixed
	/// step the stage runs: fixed-step logic → <see cref="Step"/> → <see cref="DeliverEvents"/> → sync.
	/// When something else owns the physics step, it calls Stage.FixedTickBegin, steps, then Stage.FixedTickEnd,
	/// and <see cref="Step"/> is never called.
	/// </summary>
	public interface IStagePhysics
	{
		/// <summary>Step the simulation by the stage's (scaled) fixed delta. Only called from Stage.FixedTick.</summary>
		void Step(float fixedDeltaTime);

		/// <summary>
		/// Deliver the events recorded since the last delivery to game code. Spawning and despawning are safe here.
		/// </summary>
		void DeliverEvents();
	}
}
