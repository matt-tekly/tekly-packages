using Tekly.Common.Collections;
using UnityEngine;

namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// Frame rate from recent frame times. Every value is worked out together a couple of times a second, so they
	/// agree with each other and don't flicker.
	/// </summary>
	internal sealed class FrameStats
	{
		// About a second at 60 fps, for the average
		private const int RECENT_FRAMES = 60;

		// About ten seconds at 60 fps, so the slowest 1% is several frames rather than one
		private const int LOW_FRAMES = 600;

		private const float SUMMARY_INTERVAL = 0.5f;

		/// <summary>
		/// Average frames per second over the recent frames.
		/// </summary>
		public float Fps { get; private set; }

		/// <summary>
		/// Average frame time over the recent frames, in milliseconds.
		/// </summary>
		public float FrameMs { get; private set; }

		/// <summary>
		/// Frames per second over the slowest 1% of frames.
		/// </summary>
		public float OnePercentLow { get; private set; }

		private readonly RollingWindow m_recent = new(RECENT_FRAMES);
		private readonly RollingWindow m_low = new(LOW_FRAMES, true);
		private float m_untilSummary;

		public void Tick()
		{
			var frameMs = Time.unscaledDeltaTime * 1000f;

			m_recent.Add(frameMs);
			m_low.Add(frameMs);

			m_untilSummary -= Time.unscaledDeltaTime;

			if (m_untilSummary <= 0f) {
				m_untilSummary = SUMMARY_INTERVAL;
				Summarize();
			}
		}

		public void Reset()
		{
			m_recent.Reset();
			m_low.Reset();
			m_untilSummary = 0f;
			Fps = FrameMs = OnePercentLow = 0f;
		}

		private void Summarize()
		{
			FrameMs = m_recent.GetRollingAverage();
			Fps = ToFps(FrameMs);
			OnePercentLow = ToFps(m_low.GetPercentile(0.99f));
		}

		private static float ToFps(float frameMs)
		{
			return frameMs > 0f ? 1000f / frameMs : 0f;
		}
	}
}
