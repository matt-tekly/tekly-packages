namespace Tekly.Stagecraft
{
	/// <summary>
	/// Why an actor is being activated. Readable as Actor.SpawnReason from Initialize/OnSpawned on, so a
	/// behaviour can skip spawn-only work (effects, sounds, death-effect spawners) when it is being restored from
	/// a save or created by a replicated spawn rather than spawned fresh.
	/// </summary>
	public enum SpawnReason
	{
		/// <summary>A new actor entering the game.</summary>
		Spawned,

		/// <summary>An actor whose state is being restored (save game, late join). Its behaviours' state has been set before Activate.</summary>
		Restored,

		/// <summary>An actor placed in the scene, activated by its StageScene.</summary>
		Placed
	}
}
