using System.Collections.Generic;

namespace Tekly.DevBoard
{
	/// <summary>
	/// The behaviours one Board (or DevBoard itself) ticks. Removing only clears the slot and the list is compacted
	/// before the next tick, so destroying a page full of widgets is cheap and removing during a tick is safe.
	/// Behaviours added during a tick are ticked in that same pass.
	/// </summary>
	internal class TickGroup
	{
		private readonly List<DevBoardBehaviour> m_behaviours = new();
		private int m_removedCount;

		public int Count => m_behaviours.Count - m_removedCount;

		public void Add(DevBoardBehaviour behaviour)
		{
			behaviour.TickIndex = m_behaviours.Count;
			m_behaviours.Add(behaviour);
		}

		public void Remove(DevBoardBehaviour behaviour)
		{
			var index = behaviour.TickIndex;

			if (index < 0 || index >= m_behaviours.Count || !ReferenceEquals(m_behaviours[index], behaviour)) {
				return;
			}

			m_behaviours[index] = null;
			behaviour.TickIndex = -1;
			m_removedCount++;
		}

		public void Tick()
		{
			if (m_removedCount > 0) {
				Compact();
			}

			// Count is read every iteration so behaviours added mid-tick are included
			for (var i = 0; i < m_behaviours.Count; i++) {
				var behaviour = m_behaviours[i];

				if (behaviour is not null) {
					behaviour.RunTick();
				}
			}
		}

		private void Compact()
		{
			var write = 0;

			for (var read = 0; read < m_behaviours.Count; read++) {
				var behaviour = m_behaviours[read];

				if (behaviour is null) {
					continue;
				}

				behaviour.TickIndex = write;
				m_behaviours[write] = behaviour;
				write++;
			}

			m_behaviours.RemoveRange(write, m_behaviours.Count - write);
			m_removedCount = 0;
		}
	}
}
