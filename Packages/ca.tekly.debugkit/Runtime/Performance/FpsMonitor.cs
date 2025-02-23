using Tekly.Common.Collections;
using UnityEngine;

namespace Tekly.DebugKit.Performance
{
	public class FpsMonitor
	{
		private const int ONE_PERCENT_LOW_BUFFER_SIZE = 20;
		private const int ROLLING_WINDOW_SIZE = 20;
		private const float ONE_PERCENT_LOW_SAMPLE_RATE = 0.2f;

		private readonly RollingWindow m_frameTimes = new RollingWindow(ONE_PERCENT_LOW_BUFFER_SIZE, true);
		private readonly RollingWindow m_rollingOnePercentLows = new RollingWindow(ROLLING_WINDOW_SIZE);

		private double m_totalFrameTimeMs;

		private readonly float m_onePercentLowSampleRate;
		private float m_onePercentLowSampleTime;

		public FpsMonitor()
		{
			m_onePercentLowSampleRate = ONE_PERCENT_LOW_SAMPLE_RATE;
		}

		public void Reset()
		{
			m_totalFrameTimeMs = 0;
			m_onePercentLowSampleTime = 0;

			m_frameTimes.Reset();
			m_rollingOnePercentLows.Reset();
		}

		public void Tick(float deltaTime)
		{
			var frameTimeMs = deltaTime * 1000f;

			m_totalFrameTimeMs += frameTimeMs;

			m_frameTimes.Add(frameTimeMs);

			m_onePercentLowSampleTime += deltaTime;

			if (m_onePercentLowSampleTime >= m_onePercentLowSampleRate) {
				m_onePercentLowSampleTime -= m_onePercentLowSampleRate;

				var onePercentLow = GetOnePercentLow();
				m_rollingOnePercentLows.Add(onePercentLow);
			}
		}

		public FpsSample Sample()
		{
			var onePercentLows = m_rollingOnePercentLows.Count == 0 ? GetOnePercentLow() : m_rollingOnePercentLows.GetRollingAverage();
			return new FpsSample(m_frameTimes.GetRollingAverage(), onePercentLows, m_totalFrameTimeMs / 1000f);
		}

		private float GetOnePercentLow()
		{
			var avgSlowestFrameTime = m_frameTimes.GetPercentile(0.99f);

			if (avgSlowestFrameTime <= 0f) {
				return Application.targetFrameRate;
			}

			return 1000f / avgSlowestFrameTime;
		}
	}

	public struct FpsSample
	{
		public float Average { get; private set; }
		public float OnePercentLow { get; private set; }
		public double Duration { get; private set; }

		public FpsSample(float average, float onePercentLow, double duration)
		{
			Average = average;
			OnePercentLow = onePercentLow;
			Duration = duration;
		}

		public override string ToString()
		{
			return $"FPS Average: [{Average:N0}] 1% Lows: [{OnePercentLow:N0}] Duration [{Duration:F2}]";
		}
	}
}