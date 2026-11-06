namespace Tekly.Stagecraft
{
	/// <summary>
	/// The one lifecycle state of an actor. Only the Stage and its pools change it.
	/// Pooled → Acquired → Active → Despawning → Pooled, with Destroyed reachable from anywhere.
	/// </summary>
	public enum ActorState
	{
		/// <summary>In a pool, inactive, no id.</summary>
		Pooled,

		/// <summary>Taken from the pool but not yet activated. Inactive, no id, not ticked.</summary>
		Acquired,

		/// <summary>Alive: has an id, is ticked, resolves through handles.</summary>
		Active,

		/// <summary>Despawn was requested; not alive, not ticked, waiting for the next sync point to clean up.</summary>
		Despawning,

		/// <summary>The GameObject was destroyed. The stage forgets the actor at the next sync point.</summary>
		Destroyed
	}
}
