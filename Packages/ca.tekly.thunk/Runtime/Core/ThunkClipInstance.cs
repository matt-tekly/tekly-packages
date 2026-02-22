using System;
using System.Diagnostics;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Thunk.Core
{
	public struct ThunkClipRequest
	{
		public ThunkClipState Source;
		public float? Pitch;
		public float? Volume;
		public float? Delay;
		public float? StartTime;
	}

	public enum ThunkClipInstanceState
	{
		Normal,
		Disposed,
		FadeOutStop,
		FadeOutPause,
		FadeTo
	}

	/// <summary>
	/// An instance of a playing ThunkClip
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class ThunkClipInstance
	{
		public readonly int Id;
		public bool IsPlaying => !m_disposed && m_audioSource.IsPlaying;
		public bool IsDisposed => m_disposed;

		public float Time {
			get => m_audioSource.Time;
			set => m_audioSource.Time = value;
		}
		
		public ThunkAudioSource AudioSource => m_audioSource;
		
		public string DebuggerDisplay => $"{m_clip.Name} -> {AudioSource.Clip.GetNameSafe()}";
		public AudioClip PlayingAudioClip => AudioSource.Clip;
		public ThunkClip PlayingThunkClip => m_clip.Clip;
		public ThunkClipInstanceState State => m_state;

		private readonly ThunkEmitter m_emitter;
		private readonly ThunkAudioSource m_audioSource;
		private readonly ThunkClipState m_clip;
		
		private bool m_disposed;
		private ThunkClipInstanceState m_state;

		private readonly float m_initialVolume;
		private float m_fadeVolumeStart;
		private float m_fadeVolumeEnd;
		private float m_fadeTime;
		private float m_fadeDuration;

		public ThunkClipInstance(ThunkEmitter emitter, ThunkClipRequest request)
		{
			Id = Thunk.Instance.NextClipStateId++;
			m_emitter = emitter;
			m_clip = request.Source;

			m_audioSource = emitter.GetAudioSource();
			m_audioSource.Play(request);

			m_initialVolume = m_audioSource.Volume;
		}

		public void OnEmitterStopped()
		{
			// When the emitter is stopped or destroyed no cleanup is needed
			m_disposed = true;
		}

		public void Dispose()
		{
			if (m_disposed) {
				return;
			}

			if (m_emitter != null) {
				m_emitter.ClipInstanceDisposed(this);
			}

			m_disposed = true;
			m_state = ThunkClipInstanceState.Disposed;
		}

		public void SetVolume(float volume)
		{
			FadeToDuration(volume, 0, ThunkClipInstanceState.Normal);
		}

		public void FadeIn(float duration)
		{
			m_audioSource.Volume = 0;
			FadeToDuration(m_initialVolume, duration);
		}

		public void FadeOutStop(float duration)
		{
			FadeToDuration(0, duration, ThunkClipInstanceState.FadeOutStop);
		}

		public void FadeOutPause(float duration)
		{
			FadeToDuration(0, duration, ThunkClipInstanceState.FadeOutPause);
		}

		public void FadeToDuration(float volume, float duration, ThunkClipInstanceState state = ThunkClipInstanceState.FadeTo)
		{
			m_audioSource.Paused = false;
			
			m_fadeTime = 0;
			m_fadeDuration = Mathf.Max(duration, 0);;
			m_fadeVolumeStart = m_audioSource.Volume;
			m_fadeVolumeEnd = volume;
			m_state = state;
		}

		public void FadeToSpeed(float volume, float speed, ThunkClipInstanceState state = ThunkClipInstanceState.FadeTo)
		{
			var distance = Math.Abs(m_audioSource.Volume - volume);
			FadeToDuration(volume, distance / speed, state);
		}

		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			switch (m_state) {
				case ThunkClipInstanceState.FadeOutStop:
					if (UpdateFade(deltaTime, unscaledDeltaTime)) {
						m_state = ThunkClipInstanceState.Normal;
						Dispose();
					}
					break;
				case ThunkClipInstanceState.FadeOutPause:
					if (UpdateFade(deltaTime, unscaledDeltaTime)) {
						m_state = ThunkClipInstanceState.Normal;
						m_audioSource.Paused = true;
					}
					break;
				case ThunkClipInstanceState.FadeTo:
					if (UpdateFade(deltaTime, unscaledDeltaTime)) {
						m_state = ThunkClipInstanceState.Normal;
					}
					break;
				case ThunkClipInstanceState.Normal:
				case ThunkClipInstanceState.Disposed:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		public void UpdatePitchAndVolume()
		{
			m_audioSource.UpdatePitchAndVolume();
		}

		private bool UpdateFade(float deltaTime, float unscaledDeltaTime)
		{
			if (m_fadeDuration <= 0) {
				m_audioSource.Volume = m_fadeVolumeEnd;
				return true;
			}

			m_fadeTime += m_audioSource.UseUnscaledDeltaTime ? unscaledDeltaTime : deltaTime;
			
			var progress = Mathf.Clamp01(m_fadeTime / m_fadeDuration);
			m_audioSource.Volume = Mathf.Lerp(m_fadeVolumeStart, m_fadeVolumeEnd, progress);

			return m_fadeTime >= m_fadeDuration;
		}
	}
}