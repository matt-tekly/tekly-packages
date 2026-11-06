using System;

namespace Tekly.Stagecraft.Tests
{
	// One MonoBehaviour per file, named after the class, or Unity can't add it as a component
	public class TickBehaviour : ActorBehaviour
	{
		public int Spawned;
		public int Despawned;
		public int Ticks;
		public int FixedTicks;
		public float LastDelta;
		public SpawnReason LastSpawnReason;

		[NonSerialized] public Action<TickBehaviour> OnTick;
		[NonSerialized] public Action<TickBehaviour> OnDespawn;

		public override void OnSpawned()
		{
			Spawned++;
			LastSpawnReason = SpawnReason;
		}

		public override void OnDespawned()
		{
			Despawned++;
			OnDespawn?.Invoke(this);
		}

		public override void Tick(float deltaTime)
		{
			Ticks++;
			LastDelta = deltaTime;
			OnTick?.Invoke(this);
		}

		public override void FixedTick(float fixedDeltaTime)
		{
			FixedTicks++;
		}
	}
}
