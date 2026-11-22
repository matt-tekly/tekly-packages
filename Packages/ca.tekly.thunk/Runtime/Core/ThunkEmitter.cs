using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// Plays sounds at its own position. The AudioSources it plays through are components on its GameObject, added
	/// as needed and reused.
	/// </summary>
	public class ThunkEmitter : MonoBehaviour
	{
		/// <summary>
		/// Multiplies the volume of everything this emitter plays.
		/// </summary>
		public float Volume {
			get => m_volume;
			set {
				if (!Mathf.Approximately(m_volume, value)) {
					m_volume = value;
					ApplyToVoices();
				}
			}
		}

		/// <summary>
		/// Multiplies the pitch of everything this emitter plays.
		/// </summary>
		public float Pitch {
			get => m_pitch;
			set {
				if (!Mathf.Approximately(m_pitch, value)) {
					m_pitch = value;
					ApplyToVoices();
				}
			}
		}

		/// <summary>
		/// The AudioSource whose spatial settings are used for sounds that have no template of their own.
		/// </summary>
		public AudioSource SpatialTemplate {
			get => m_spatialTemplate;
			set => m_spatialTemplate = value;
		}

		[Tooltip("Multiplies the volume of everything this emitter plays.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_volume = 1f;

		[Tooltip("Multiplies the pitch of everything this emitter plays.")]
		[Range(0f, 3f)]
		[SerializeField] private float m_pitch = 1f;

		[Tooltip("Optional. An AudioSource whose 3D sound settings are used for what this emitter plays: one in a prefab, or a disabled one on this object. A sound with its own template uses that in place of this. Without either, sounds are not positioned.")]
		[SerializeField] private AudioSource m_spatialTemplate;

		private readonly Stack<ThunkSource> m_freeSources = new Stack<ThunkSource>();

		/// <summary>
		/// Plays the sound at this emitter. Ignore the handle to play it as a one-shot.
		/// </summary>
		public ThunkHandle Play(ThunkSound sound)
		{
			var runtime = ThunkRuntime.Instance;
			return runtime != null ? runtime.Play(sound, this) : ThunkHandle.None;
		}

		/// <summary>
		/// Stops everything this emitter is playing.
		/// </summary>
		public void StopAll(float fade = 0f)
		{
			ThunkRuntime.Instance?.StopEmitter(this, fade);
		}

		internal ThunkSource RentSource()
		{
			if (m_freeSources.TryPop(out var source)) {
				return source;
			}

			var audio = gameObject.AddComponent<AudioSource>();
			audio.playOnAwake = false;

			return new ThunkSource(audio);
		}

		internal void ReturnSource(ThunkSource source)
		{
			var audio = source.Audio;

			// Unity null check: the AudioSource is already gone if this GameObject is being destroyed
			if (audio == null) {
				return;
			}

			audio.Stop();
			audio.clip = null;
			audio.loop = false;
			audio.outputAudioMixerGroup = null;

			source.Gain = 1f;
			m_freeSources.Push(source);
		}

		private void ApplyToVoices()
		{
			ThunkRuntime.Instance?.ApplyEmitter(this);
		}

		private void OnDestroy()
		{
			ThunkRuntime.Instance?.EmitterDestroyed(this);
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			// The inspector changes the serialized fields directly, bypassing the properties
			if (Application.isPlaying) {
				ApplyToVoices();
			}
		}
#endif
	}
}
