using Tekly.DebugKit.Widgets;
using UnityEngine;

namespace Tekly.DebugKit.Performance
{
	public class FpsStat : PerformanceStat
	{
		private readonly FpsMonitor m_fpsMonitor;
		private int m_lastFps;

		private float m_lastSampleTime;

		private const float TIME_BETWEEN_SAMPLES = 1f;

		public FpsStat(FpsMonitor fpsMonitor, Container container) : base("FPS avg", "{0:F0}", container)
		{
			m_fpsMonitor = fpsMonitor;
		}

		protected override double Value {
			get {
				if (Time.realtimeSinceStartup - m_lastSampleTime > TIME_BETWEEN_SAMPLES) {
					m_lastFps = (int)(1000f / m_fpsMonitor.Sample().Average);
					m_lastSampleTime = Time.realtimeSinceStartup;
				}

				return m_lastFps;
			}
		}
	}

	public class FpsLowsStat : PerformanceStat
	{
		private readonly FpsMonitor m_fpsMonitor;
		private int m_lastFps;

		private float m_lastSampleTime;

		private const float TIME_BETWEEN_SAMPLES = 1f;

		public FpsLowsStat(FpsMonitor fpsMonitor, Container container) : base("FPS lows", "{0:F0}", container)
		{
			m_fpsMonitor = fpsMonitor;
		}

		protected override double Value {
			get {
				if (!(Time.realtimeSinceStartup - m_lastSampleTime > TIME_BETWEEN_SAMPLES)) {
					return m_lastFps;
				}

				var sample = m_fpsMonitor.Sample();
				m_lastFps = (int)sample.OnePercentLow;
				m_lastSampleTime = Time.realtimeSinceStartup;

				return m_lastFps;
			}
		}
	}
}