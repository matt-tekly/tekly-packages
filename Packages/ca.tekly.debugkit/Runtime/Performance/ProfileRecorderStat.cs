using Tekly.DebugKit.Widgets;
using Unity.Profiling;

namespace Tekly.DebugKit.Performance
{
	public class ProfileRecorderStat : PerformanceStat
	{
		private ProfilerRecorder m_recorder;
		private int m_lastValue;

		public ProfileRecorderStat(string name, ProfilerCategory category, string profiler, Container container, int count = 1) : base(name, "{0:F0}", container)
		{
			m_recorder = ProfilerRecorder.StartNew(category, profiler, count);
		}

		protected override double Value {
			get {
				if (!m_recorder.Valid) {
					return m_lastValue;
				}

				if (m_recorder.Count > 1) {
					return (int)GetRecorderFrameAverage(m_recorder);
				}

				if (m_recorder.UnitType == ProfilerMarkerDataUnit.Bytes) {
					// Convert everything to MB - this will mean things will report as 0 sometimes
					m_lastValue = (int)(m_recorder.LastValue / 1024 / 1024);
				} else {
					m_lastValue = (int)m_recorder.LastValue;
				}

				return m_lastValue;
			}
		}

		public override void Dispose()
		{
			m_recorder.Dispose();
		}

		private static long GetRecorderFrameAverage(ProfilerRecorder recorder)
		{
			var samplesCount = recorder.Capacity;
			if (samplesCount == 0) {
				return 0;
			}

			long r = 0;
			unsafe {
				var samples = stackalloc ProfilerRecorderSample[samplesCount];
				recorder.CopyTo(samples, samplesCount);

				for (var i = 0; i < samplesCount; ++i) {
					r += samples[i].Value;
				}

				r /= samplesCount;
			}

			return r;
		}
	}
}