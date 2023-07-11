using Tekly.Common.Registrys;
using UnityEngine;

namespace Tekly.Common.Timers
{
	[CreateAssetMenu(menuName = "Tekly/Timer")]
	public class TimerRef : RegisterableValue<Timer>
	{
		public override IRegistry<Timer> Registry => TimerRegistry.Instance;

		public virtual float Scale {
			get => Value.Scale;
			set => Value.Scale = value;
		}

		public float DeltaTime => Value.DeltaTime;
		public float FixedDeltaTime => Value.FixedDeltaTime;
	}
	
	public class TimerRegistry : SingletonRegistry<Timer, TimerRegistry> { }
}