using System;
using System.Diagnostics;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Thunk.Core
{
	public struct ThunkClipRequest
	{
		public ThunkClipState Source;
		
		/// <summary>
		/// The AudioClip selected by the ThunkClipState. If null the source selects one when played.
		/// </summary>
		public AudioClip AudioClip;
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
			set {
				if (!m_disposed) {
					m_audioSource.Time = value;
				}
			}
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

		/// <summary>
		/// The volume FadeIn returns to. Starts as the requested volume and is updated by SetVolume.
		/// </summary>
		private float m_baseVolume;
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

			m_baseVolume = m_audioSource.Volume;
		}

		/// <summary>
		/// Called when the owning emitter is destroyed. Its AudioSources are destroyed with it, so they are not
		/// returned to the pool, but the instance is still removed from its ThunkClipState.
		/// </summary>
		public void OnEmitterStopped()
		{
			Dispose(false);
		}

		/// <summary>
		/// Stops the instance, removes it from its ThunkClipState and emitter, and returns its AudioSource to the pool.
		/// This is the only way an instance should be stopped.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		private void Dispose(bool returnToEmitter)
		{
			if (m_disposed) {
				return;
			}

			m_disposed = true;
			m_state = ThunkClipInstanceState.Disposed;

			m_clip.InstanceDisposed(this);

			if (returnToEmitter && m_emitter != null) {
				m_emitter.ClipInstanceDisposed(this);
			}
		}

		/// <summary>
		/// Sets the volume this instance plays at, which is also the volume FadeIn returns to.
		/// - Normal: applied immediately.
		/// - FadeTo (including FadeIn): the fade is cancelled and the volume applied immediately.
		/// - FadeOutStop / FadeOutPause: the fade out continues; the volume is only stored, so a paused
		///   instance resumes at it when faded back in.
		/// </summary>
		public void SetVolume(float volume)
		{
			if (m_disposed) {
				return;
			}

			m_baseVolume = volume;

			if (m_state == ThunkClipInstanceState.FadeOutStop || m_state == ThunkClipInstanceState.FadeOutPause) {
				return;
			}

			m_state = ThunkClipInstanceState.Normal;
			m_audioSource.Volume = volume;
		}

		public void FadeIn(float duration)
		{
			if (m_disposed) {
				return;
			}

			m_audioSource.Volume = 0;
			FadeToDuration(m_baseVolume, duration);
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
			if (m_disposed) {
				return;
			}

			m_audioSource.Paused = false;
			
			m_fadeTime = 0;
			m_fadeDuration = Mathf.Max(duration, 0);;
			m_fadeVolumeStart = m_audioSource.Volume;
			m_fadeVolumeEnd = volume;
			m_state = state;
		}

		public void FadeToSpeed(float volume, float speed, ThunkClipInstanceState state = ThunkClipInstanceState.FadeTo)
		{
			if (m_disposed) {
				return;
			}

			var distance = Math.Abs(m_audioSource.Volume - volume);
			FadeToDuration(volume, distance / speed, state);
		}

		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			// A disposed instance's AudioSource may already belong to another instance, or be destroyed
			if (m_disposed) {
				return;
			}

			switch (m_state) {
				case ThunkClipInstanceState.FadeOutStop:
					if (UpdateFade(deltaTime, unscaledDeltaTime)) {
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
			if (m_disposed) {
				return;
			}

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