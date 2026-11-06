using System;
using System.Collections.Generic;

namespace Tekly.Stagecraft.Tests
{
	public class PhaseLogger : ActorBehaviour
	{
		[NonSerialized] public List<string> Log;

		public override void FixedTick(float fixedDeltaTime)
		{
			Log?.Add("fixed");
		}

		public override void OnDespawned()
		{
			Log?.Add("despawned");
		}
	}
}
