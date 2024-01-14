using Tekly.Common.Collections;
using UnityEngine;

namespace Tekly.Common.Utils
{
	/// <summary>
	/// Tracks FPS
	/// </summary>
	public class Framerate
	{
		public int Fps;
		public int FpsLast;

		private float m_timer;

		private readonly FixedRing<float> m_deltaTimes;
		private readonly float m_timeBetweenUpdates;

		public Framerate(int frames = 60, float timeBetweenUpdates = 0.5f)
		{
			m_deltaTimes = new FixedRing<float>(frames);
			m_timeBetweenUpdates = timeBetweenUpdates;
		}

		/// <summary>
		/// Returns true if the FPS changed
		/// </summary>
		public bool Tick()
		{
			var deltaTime = Time.deltaTime;
			m_deltaTimes.Add(deltaTime);

			m_timer += deltaTime;
			if (m_timer < m_timeBetweenUpdates) {
				return false;
			}

			m_timer -= m_timeBetweenUpdates;
			var total = 0f;

			foreach (var delta in m_deltaTimes) {
				total += delta;
			}

			FpsLast = Fps;
			Fps = Mathf.FloorToInt(m_deltaTimes.Count / total);

			return Fps != FpsLast;
		}
	}
}