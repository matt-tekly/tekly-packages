using System;
using System.Collections.Generic;

namespace Tekly.Stagecraft.Tests
{
	public class TestSystem : ActorSystem<SystemBehaviour>
	{
		public readonly List<SystemBehaviour> Added = new List<SystemBehaviour>();
		public readonly List<SystemBehaviour> Removed = new List<SystemBehaviour>();
		public int TickCalls;
		public int FixedTickCalls;
		public int LastLiveCount;
		public int LastRawCount;
		public Action<ActorList<SystemBehaviour>> OnTick;

		public override void OnAdded(SystemBehaviour behaviour)
		{
			Added.Add(behaviour);
		}

		public override void OnRemoved(SystemBehaviour behaviour)
		{
			Removed.Add(behaviour);
		}

		public override void Tick(ActorList<SystemBehaviour> behaviours, float deltaTime)
		{
			TickCalls++;
			LastRawCount = behaviours.RawCount;
			LastLiveCount = 0;
			foreach (var behaviour in behaviours) {
				LastLiveCount++;
				behaviour.Value++;
			}

			OnTick?.Invoke(behaviours);
		}

		public override void FixedTick(ActorList<SystemBehaviour> behaviours, float fixedDeltaTime)
		{
			FixedTickCalls++;
		}
	}

	public struct TestArgs
	{
		public int Number;
		public float Speed;
	}

	public class TestService
	{
		public int Value = 42;
	}

	/// <summary>Records the order of stage phases around a fake physics step.</summary>
	public class FakePhysics : IStagePhysics
	{
		public readonly List<string> Log = new List<string>();
		public Action OnDeliver;

		public void Step(float fixedDeltaTime)
		{
			Log.Add("step");
		}

		public void DeliverEvents()
		{
			Log.Add("deliver");
			OnDeliver?.Invoke();
		}
	}
}
