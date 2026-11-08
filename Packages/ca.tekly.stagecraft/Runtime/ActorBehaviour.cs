using UnityEngine;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// A component of an <see cref="Actor"/>. Override OnCreated for once-per-instance setup, OnSpawned/OnDespawned
	/// for per-spawn lifecycle, Tick/FixedTick to be ticked (only types that override them are scheduled), or leave
	/// the ticks alone and let an ActorSystem drive it. Don't use Awake, OnEnable, OnDisable or Update: the stage
	/// owns the lifecycle and the clock, and Unity's messages fire at times it doesn't control.
	/// </summary>
	public abstract class ActorBehaviour : MonoBehaviour
	{
		public Actor Actor { get; internal set; }
		public Stage Stage => Actor.Stage;
		public ActorId Id => Actor.Id;
		public bool IsAlive => Actor.State == ActorState.Active;

		/// <summary>Why this activation happened; check it in OnSpawned to skip spawn-only work on a restore.</summary>
		public SpawnReason SpawnReason => Actor.SpawnReason;

		// Set once per pooled instance by the stage; where this behaviour lives in its tick group's lists
		internal TypeInfo TypeInfo;
		internal int AllIndex = -1;
		internal int TickIndex = -1;
		internal int FixedIndex = -1;

		/// <summary>
		/// Called once, when the pooled instance is created: after injection, before the first spawn, on the inactive
		/// object. Cache components and resolve services here instead of in Awake.
		/// </summary>
		public virtual void OnCreated() { }

		/// <summary>Called inside Spawn/Activate, after spawn args. The actor is alive. Raw references handed out here are valid until the next sync.</summary>
		public virtual void OnSpawned() { }

		/// <summary>Called at the sync point after Despawn. The actor is not alive but still has its id and transform.</summary>
		public virtual void OnDespawned() { }

		/// <summary>Presentation tick, from Stage.Tick. Only scheduled when overridden.</summary>
		public virtual void Tick(float deltaTime) { }

		/// <summary>Gameplay tick, from Stage.FixedTick. Only scheduled when overridden.</summary>
		public virtual void FixedTick(float fixedDeltaTime) { }

		/// <summary>Despawn the whole actor. Deferred: see Stage.Despawn.</summary>
		public void Despawn()
		{
			Actor.Despawn();
		}
	}
}
