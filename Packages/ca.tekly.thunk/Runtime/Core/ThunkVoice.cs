using System.Collections.Generic;
using Tekly.Common.Utils.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Thunk
{
	internal enum ThunkVoiceState
	{
		Playing,

		/// <summary>Fading out, ends when the fade does. Nothing can bring it back.</summary>
		Stopping,

		/// <summary>Fading out, pauses when the fade does.</summary>
		Pausing,

		Paused
	}

	/// <summary>
	/// One AudioSource playing one clip on behalf of a voice.
	/// </summary>
	internal sealed class ThunkSource
	{
		public readonly AudioSource Audio;

		/// <summary>
		/// Volume of this source relative to the rest of its voice.
		/// </summary>
		public float Gain = 1f;

		/// <summary>
		/// Pitch of this source relative to the rest of its voice.
		/// </summary>
		public float PitchScale = 1f;

		/// <summary>
		/// When the source starts on the audio clock, for sounds that schedule their sources.
		/// </summary>
		public double StartDsp;

		/// <summary>
		/// The template the AudioSource's spatial settings were last copied from, null if it is not positioned.
		/// </summary>
		public AudioSource Template;

		// Fading this one source out, see ThunkVoice.FadeOutSource
		public float FadeFrom;
		public float FadeTime;
		public float FadeDuration;

		public ThunkSource(AudioSource audio)
		{
			Audio = audio;
		}
	}

	/// <summary>
	/// One playing sound: what a <see cref="ThunkHandle"/> refers to. A voice owns one or more sources and applies
	/// its volume, pitch and fade to all of them. Voices are pooled and owned by the <see cref="ThunkRuntime"/>.
	/// </summary>
	internal sealed class ThunkVoice
	{
		public ThunkHandle Handle => new ThunkHandle(Index, Generation);
		public bool IsPaused => State == ThunkVoiceState.Paused || State == ThunkVoiceState.Pausing;
		public bool IsFading => m_fadeDuration > 0f;
		public bool HasSourceFades => m_sourceFades > 0;

		/// <summary>
		/// The pitch the voice's sources are playing at.
		/// </summary>
		public float Pitch => BasePitch * UserPitch * Emitter.Pitch;

		/// <summary>
		/// How loud the voice is before its emitter and distance are applied. Used to pick the quietest voice.
		/// </summary>
		public float Loudness => BaseVolume * UserVolume * Fade;

		public readonly int Index;
		public readonly List<ThunkSource> Sources = new List<ThunkSource>(2);

		public uint Generation = 1;
		public bool Active;
		public int ActiveIndex;
		public long Order;

		public ThunkSound Sound;
		public ThunkSoundRecord Record;
		public ThunkEmitter Emitter;
		public ThunkTrackState Track;
		public AudioMixerGroup MixerGroup;

		/// <summary>
		/// The AudioSource the voice's spatial settings are copied from. Null for a voice with no position.
		/// </summary>
		public AudioSource SpatialTemplate;

		public ThunkVoiceState State;

		/// <summary>
		/// Set once the voice has been released, so it only happens once.
		/// </summary>
		public bool Released;

		// For sounds that move through stages. What the values mean is up to the sound.
		public int Stage;
		public double StageDsp;
		public double StageBeginDsp;

		/// <summary>
		/// Has the sound told when the voice's pitch changes, for sounds that schedule by clip length.
		/// </summary>
		public bool WatchPitch;
		public bool Unscaled;

		public float BaseVolume;
		public float BasePitch;
		public float UserVolume;
		public float UserPitch;
		public float Fade;

		/// <summary>
		/// Set by the track the voice is music on, to duck it.
		/// </summary>
		public float TrackGain;

		private float m_fadeFrom;
		private float m_fadeTo;
		private float m_fadeTime;
		private float m_fadeDuration;
		private Ease m_fadeEase;
		private int m_sourceFades;
		private float m_watchedPitch;

		public ThunkVoice(int index)
		{
			Index = index;
		}

		/// <summary>
		/// Takes an AudioSource from the emitter and sets it up with the clip and the sound's settings. The caller
		/// starts it.
		/// </summary>
		public ThunkSource AddSource(AudioClip clip, float gain = 1f)
		{
			var source = Emitter.RentSource();
			source.Gain = gain;

			var audio = source.Audio;
			audio.clip = clip;
			audio.loop = false;
			audio.outputAudioMixerGroup = MixerGroup;
			audio.priority = Sound.Priority;
			audio.ignoreListenerPause = Sound.PlayWhilePaused;

			ThunkSpatialTemplate.Apply(source, SpatialTemplate);

			Sources.Add(source);
			Apply(source);

			return source;
		}

		public void Apply()
		{
			for (var index = 0; index < Sources.Count; index++) {
				Apply(Sources[index]);
			}

			if (WatchPitch) {
				var pitch = Pitch;

				if (!Mathf.Approximately(pitch, m_watchedPitch)) {
					m_watchedPitch = pitch;
					Sound.PitchChanged(this);
				}
			}
		}

		/// <summary>
		/// Starts telling the sound about pitch changes, from the pitch the voice has now.
		/// </summary>
		public void StartWatchingPitch()
		{
			WatchPitch = true;
			m_watchedPitch = Pitch;
		}

		/// <summary>
		/// Fades one source out and stops it, leaving the rest of the voice playing.
		/// </summary>
		public void FadeOutSource(ThunkSource source, float duration)
		{
			if (duration <= 0f) {
				source.Audio.Stop();
				return;
			}

			if (source.FadeDuration <= 0f) {
				m_sourceFades++;
			}

			source.FadeFrom = source.Gain;
			source.FadeTime = 0f;
			source.FadeDuration = duration;
		}

		public void TickSourceFades(float deltaTime)
		{
			for (var index = 0; index < Sources.Count; index++) {
				var source = Sources[index];

				if (source.FadeDuration <= 0f) {
					continue;
				}

				source.FadeTime += deltaTime;

				var ratio = Mathf.Clamp01(source.FadeTime / source.FadeDuration);
				source.Gain = Mathf.Lerp(source.FadeFrom, 0f, ratio);
				Apply(source);

				if (ratio >= 1f) {
					source.Audio.Stop();
					source.FadeDuration = 0f;
					m_sourceFades--;
				}
			}
		}

		public void StartFade(float to, float duration, Ease ease)
		{
			if (duration <= 0f) {
				m_fadeDuration = 0f;
				Fade = to;
				Apply();
				return;
			}

			m_fadeFrom = Fade;
			m_fadeTo = to;
			m_fadeTime = 0f;
			m_fadeDuration = duration;
			m_fadeEase = ease;
		}

		/// <summary>
		/// Advances the fade. Returns true on the tick the fade completes.
		/// </summary>
		public bool TickFade(float deltaTime)
		{
			m_fadeTime += deltaTime;

			var ratio = Mathf.Clamp01(m_fadeTime / m_fadeDuration);
			Fade = Mathf.LerpUnclamped(m_fadeFrom, m_fadeTo, Easing.Evaluate(ratio, m_fadeEase));

			var finished = ratio >= 1f;

			if (finished) {
				Fade = m_fadeTo;
				m_fadeDuration = 0f;
			}

			Apply();
			return finished;
		}

		/// <summary>
		/// True once every source has played out. AudioSource.isPlaying is false while audio is suspended by the
		/// application or by AudioListener.pause, so a suspended source still counts as playing.
		/// </summary>
		public bool IsFinished(bool applicationPaused)
		{
			if (applicationPaused) {
				return false;
			}

			for (var index = 0; index < Sources.Count; index++) {
				var audio = Sources[index].Audio;

				if (audio.isPlaying || (AudioListener.pause && !audio.ignoreListenerPause)) {
					return false;
				}
			}

			return true;
		}

		public void PauseSources()
		{
			for (var index = 0; index < Sources.Count; index++) {
				Sources[index].Audio.Pause();
			}
		}

		public void UnPauseSources()
		{
			for (var index = 0; index < Sources.Count; index++) {
				Sources[index].Audio.UnPause();
			}
		}

		public void Reset()
		{
			Sources.Clear();

			Sound = null;
			Record = null;
			Emitter = null;
			Track = null;
			MixerGroup = null;
			SpatialTemplate = null;

			State = ThunkVoiceState.Playing;
			Released = false;
			Stage = 0;
			StageDsp = 0d;
			StageBeginDsp = 0d;
			WatchPitch = false;

			m_fadeDuration = 0f;
			m_sourceFades = 0;
		}

		public void Apply(ThunkSource source)
		{
			source.Audio.volume = BaseVolume * UserVolume * Fade * TrackGain * source.Gain * Emitter.Volume;
			source.Audio.pitch = Pitch * source.PitchScale;
		}
	}
}
