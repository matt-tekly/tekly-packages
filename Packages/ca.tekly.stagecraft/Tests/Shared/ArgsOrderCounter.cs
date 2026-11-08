namespace Tekly.Stagecraft.Tests
{
	/// <summary>Records which Unity and stage callbacks had the spawn args available when they ran.</summary>
	public class ArgsOrderCounter : ActorBehaviour, ISpawnArgs<TestArgs>
	{
		public int Number;
		public bool WasActiveInInitialize;
		public bool HadArgsInAwake;
		public bool HadArgsInEnable;
		public bool HadArgsInSpawned;

		public void Initialize(in TestArgs args)
		{
			Number = args.Number;
			WasActiveInInitialize = gameObject.activeInHierarchy;
		}

		private void Awake()
		{
			HadArgsInAwake = Number != 0;
		}

		private void OnEnable()
		{
			HadArgsInEnable = Number != 0;
		}

		public override void OnSpawned()
		{
			HadArgsInSpawned = Number != 0;
		}
	}
}
