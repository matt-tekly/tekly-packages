using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// Plays sounds at its own position. The AudioSources it plays through are pooled centrally and follow it, so
	/// a sound is not cut off when the emitter goes away: once the emitter is disabled or destroyed, sounds that
	/// end on their own finish where the emitter last was and sounds that loop are released.
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
		/// The template for sounds that have no template of their own. They play through copies of it.
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

		[Tooltip("Optional. An AudioSource in a prefab. Sounds from this emitter play through copies of that prefab, so its 3D sound settings and any filters on it apply. A sound with its own template uses that one instead. With neither, sounds are not positioned.")]
		[SerializeField] private AudioSource m_spatialTemplate;

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

		private void ApplyToVoices()
		{
			ThunkRuntime.Instance?.ApplyEmitter(this);
		}

		private void OnDisable()
		{
			ThunkRuntime.Instance?.EmitterGone(this);
		}

		private void OnDestroy()
		{
			// A component that was never enabled doesn't get OnDisable
			ThunkRuntime.Instance?.EmitterGone(this);
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
