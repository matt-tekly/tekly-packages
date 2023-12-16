using Tekly.Lofi.Emitters;

namespace Tekly.Lofi.Core
{
	/// Music is played on a track. A track can cross fade between music. 
	public class LofiTrack
	{
		private readonly LofiEmitter m_emitter;

		private int m_activeRunner;

		public LofiTrack(LofiEmitter emitter)
		{
			m_emitter = emitter;
		}

		public void Stop()
		{
			if (m_activeRunner != Constants.INVALID_ID) {
				m_emitter.Stop(m_activeRunner);
				m_activeRunner = Constants.INVALID_ID;
			}
		}

		public void Play(LofiClip clip)
		{
			Stop();
			m_activeRunner = m_emitter.Play(clip);
		}

		public void CrossFade(LofiClip clip, float duration)
		{
			if (m_activeRunner != Constants.INVALID_ID) {
				m_emitter.FadeOut(m_activeRunner, duration);
				m_activeRunner = Constants.INVALID_ID;
			}

			m_activeRunner = m_emitter.Play(clip);
			m_emitter.FadeIn(m_activeRunner, duration);
		}
	}
}