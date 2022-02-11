using UnityEngine;

namespace Tekly.Webster.Utility
{
	public class FpsMeter
	{
		const float MeasurePeriod = 0.5f;
		private int m_accumulator;
		private float m_nextPeriod;
		private int m_currentFps;

		public FpsMeter()
		{
			m_nextPeriod = Time.realtimeSinceStartup + MeasurePeriod;
		}
		
		public void Update()
		{
			m_accumulator++;
			
			if (!(Time.realtimeSinceStartup > m_nextPeriod)) {
				return;
			}

			m_currentFps = (int) (m_accumulator / MeasurePeriod);
			m_accumulator = 0;
			m_nextPeriod += MeasurePeriod;
		}
	}
}