namespace Tekly.Stagecraft.Tests
{
	public class LifeCounter : ActorBehaviour
	{
		public int Awakes;
		public int Enables;
		public int Spawns;
		public int Despawns;

		private void Awake()
		{
			Awakes++;
		}

		private void OnEnable()
		{
			Enables++;
		}

		public override void OnSpawned()
		{
			Spawns++;
		}

		public override void OnDespawned()
		{
			Despawns++;
		}
	}
}
