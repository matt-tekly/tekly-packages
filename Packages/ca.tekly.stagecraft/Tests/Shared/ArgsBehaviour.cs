using System;

namespace Tekly.Stagecraft.Tests
{
	public class ArgsBehaviour : ActorBehaviour, ISpawnArgs<TestArgs>
	{
		[NonSerialized] public TestArgs Args;
		public bool ArgsSeenInOnSpawned;
		public int Initialized;

		public void Initialize(in TestArgs args)
		{
			Args = args;
			Initialized++;
		}

		public override void OnSpawned()
		{
			ArgsSeenInOnSpawned = Args.Number != 0;
		}
	}
}
