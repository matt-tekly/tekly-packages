using System;
using Tekly.Lofi.Emitters;
using UnityEngine;

namespace Tekly.Lofi.Core
{
	public struct LofiClipRunnerData
	{
		public LofiClip SourceClip;
		public AudioClip Clip;
		public LofiMixerGroup MixerGroup;
		
		public float Volume;
		public float Pitch;
		public bool Loop;
	}

	public enum RunnerState
	{
		Normal,
		Disposed,
		FadeIn,
		FadeOut
	}
	
	/// A running instance of a LofiClip
	public class LofiClipRunner
	{
		public readonly int Id;
		public bool IsComplete => m_isDisposed || !m_audioSource.IsPlaying;

		public LofiClip Clip => m_clip;
		
		private readonly LofiEmitter m_emitter;
		private readonly LofiAudioSource m_audioSource;
		private readonly LofiClip m_clip;

		private bool m_isDisposed;
		
		private readonly float m_initialVolume;
		
		private float m_fadeVolumeStart;
		private float m_fadeVolumeEnd;
		
		private float m_fadeTime;
		private float m_fadeDuration;

		private RunnerState m_state;
		
		private static int s_id;
		
		public LofiClipRunner(LofiEmitter emitter, LofiClipRunnerData data)
		{
			Id = s_id++;
			
			m_emitter = emitter;
			m_clip = data.SourceClip;
			
			m_audioSource = m_emitter.GetAudioSource();
			m_audioSource.Volume = data.Volume;
			m_audioSource.Pitch = data.Pitch;
			m_audioSource.Clip = data.Clip;
			m_audioSource.MixerGroup = data.MixerGroup;
			m_audioSource.Loop = data.Loop;

			m_initialVolume = data.Volume;

			m_audioSource.Play();
		}
		
		public void Dispose()
		{
			if (m_isDisposed) {
				return;
			}
			
			m_emitter.FreeAudioSource(m_audioSource);
			m_clip.RunnerCompleted(this);
			m_isDisposed = true;
		}

		public void Stop()
		{
			m_audioSource.Stop();
		}

		public void FadeIn(float duration)
		{
			m_fadeTime = 0;
			m_fadeDuration = duration;
			
			m_fadeVolumeStart = 0;
			m_fadeVolumeEnd = m_initialVolume;

			m_state = RunnerState.FadeIn;
		}
		
		public void FadeOut(float duration)
		{
			m_fadeTime = 0;
			m_fadeDuration = duration;
			
			m_fadeVolumeStart = m_audioSource.Volume;
			m_fadeVolumeEnd = 0;

			m_state = RunnerState.FadeOut;
		}

		public void Update()
		{
			switch (m_state) {
				case RunnerState.Disposed:
					break;
				case RunnerState.Normal:
					break;
				case RunnerState.FadeIn:
					m_fadeTime += Time.deltaTime;
					m_audioSource.Volume = Mathf.Lerp(m_fadeVolumeStart, m_fadeVolumeEnd, m_fadeTime / m_fadeDuration);

					if (m_fadeTime >= m_fadeDuration) {
						m_state = RunnerState.Normal;
					}
					break;
				case RunnerState.FadeOut:
					m_fadeTime += Time.deltaTime;
					m_audioSource.Volume = Mathf.Lerp(m_fadeVolumeStart, m_fadeVolumeEnd, m_fadeTime / m_fadeDuration);

					if (m_fadeTime >= m_fadeDuration) {
						m_state = RunnerState.Normal;
						Stop();
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}