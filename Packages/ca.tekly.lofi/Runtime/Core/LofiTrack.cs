using System;
using Tekly.Lofi.Emitters;

namespace Tekly.Lofi.Core
{
	public enum LofiTrackMode
	{
		Default,
		Loop,
	}
	
	/// Music is played on a track. A track can cross fade between music. 
	public class LofiTrack
	{
		private readonly LofiEmitter m_emitter;
		
		private int m_activeRunner = Constants.INVALID_ID;
		private LofiTrackMode m_mode = LofiTrackMode.Default;

		private LofiClip m_loopClip;
		private float m_crossFadeDuration;

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

			m_loopClip = null;
			m_mode = LofiTrackMode.Default;
		}

		public void Play(LofiClip clip)
		{
			Stop();
			m_activeRunner = m_emitter.Play(clip);
		}

		public void CrossFade(LofiClip clip, float duration)
		{
			m_loopClip = null;
			m_mode = LofiTrackMode.Default;
			
			if (m_activeRunner != Constants.INVALID_ID) {
				m_emitter.FadeOut(m_activeRunner, duration);
				m_activeRunner = Constants.INVALID_ID;
			}

			m_activeRunner = m_emitter.Play(clip);
			m_emitter.FadeIn(m_activeRunner, duration);
		}

		public void FadeOut(float duration)
		{
			m_loopClip = null;
			m_mode = LofiTrackMode.Default;
			
			if (m_activeRunner != Constants.INVALID_ID) {
				m_emitter.FadeOut(m_activeRunner, duration);
				m_activeRunner = Constants.INVALID_ID;
			}
		}

		public void FadeToDuration(float volume, float duration)
		{
			m_loopClip = null;
			m_mode = LofiTrackMode.Default;
			
			if (m_activeRunner != Constants.INVALID_ID) {
				m_emitter.FadeToDuration(m_activeRunner, volume, duration);
			}
		}
		
		public void FadeToSpeed(float volume, float speed)
		{
			m_loopClip = null;
			m_mode = LofiTrackMode.Default;
			
			if (m_activeRunner != Constants.INVALID_ID) {
				m_emitter.FadeToSpeed(m_activeRunner, volume, speed);
			}
		}
		
		public void Loop(LofiClip clip, float crossFadeDuration, bool immediate = false)
		{
			if (immediate) {
				Stop();	
			} else {
				if (m_activeRunner != Constants.INVALID_ID) {
					m_emitter.FadeOut(m_activeRunner, crossFadeDuration / 2f);
					m_activeRunner = Constants.INVALID_ID;
				}
			}
			
			m_loopClip = clip;
			m_mode = LofiTrackMode.Loop;
			m_crossFadeDuration = crossFadeDuration;

			m_activeRunner = m_emitter.Play(clip);
			m_emitter.FadeIn(m_activeRunner, m_crossFadeDuration / 2f);
		}

		public void Tick()
		{
			switch (m_mode) {
				case LofiTrackMode.Default:
					break;
				case LofiTrackMode.Loop:
					if (m_activeRunner != Constants.INVALID_ID) {
						var timeRemaining = m_emitter.GetTimeRemaining(m_activeRunner);

						if (timeRemaining <= m_crossFadeDuration / 2f) {
							m_emitter.FadeOut(m_activeRunner, m_crossFadeDuration / 2f);
							m_activeRunner = m_emitter.Play(m_loopClip);
							m_emitter.FadeIn(m_activeRunner, m_crossFadeDuration / 2f);
						}
					}
					
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}