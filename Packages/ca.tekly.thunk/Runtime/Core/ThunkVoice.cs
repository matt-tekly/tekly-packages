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
		/// The template the AudioSource's spatial settings were last copied from, null if it is not positioned.
		/// </summary>
		public AudioSource Template;

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
			m_fadeDuration = 0f;
		}

		private void Apply(ThunkSource source)
		{
			source.Audio.volume = BaseVolume * UserVolume * Fade * TrackGain * source.Gain * Emitter.Volume;
			source.Audio.pitch = BasePitch * UserPitch * Emitter.Pitch;
		}
	}
}
