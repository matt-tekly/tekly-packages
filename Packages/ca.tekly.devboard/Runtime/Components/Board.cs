using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// A top-level container. Ticks every enabled widget inside it, every frame by default or at a lower
	/// rate set with WithTickRate (handy for overlays that only show a few numbers).
	/// </summary>
	public class Board : ContainerWidget
	{
		/// <summary>
		/// Board names scope the saved view state (foldouts, scroll positions) of everything inside them.
		/// </summary>
		public override string StateScope {
			get => string.IsNullOrEmpty(base.StateScope) ? name : base.StateScope;
			set => base.StateScope = value;
		}

		internal TickGroup TickGroup { get; } = new();

		private float m_tickInterval;
		private float m_nextTickTime;

		/// <summary>
		/// How many times per second widgets refresh. Zero or less ticks every frame.
		/// </summary>
		public Board WithTickRate(float ticksPerSecond)
		{
			m_tickInterval = ticksPerSecond > 0f ? 1f / ticksPerSecond : 0f;
			m_nextTickTime = 0f;
			return this;
		}

		private void Update()
		{
			var now = Time.realtimeSinceStartup;

			if (now < m_nextTickTime) {
				return;
			}

			m_nextTickTime = now + m_tickInterval;
			TickGroup.Tick();
		}
	}
}
