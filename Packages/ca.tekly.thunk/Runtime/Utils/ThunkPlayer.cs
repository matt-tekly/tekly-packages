using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.Thunk
{
	public enum ThunkPlayerDisableAction
	{
		/// <summary>The sound plays on.</summary>
		Nothing,

		/// <summary>The sound stops immediately.</summary>
		Stop,

		/// <summary>The sound ends naturally, using its fade out.</summary>
		Release
	}

	/// <summary>
	/// Plays a sound. With an emitter the sound plays at the emitter, without one it has no position.
	/// </summary>
	public class ThunkPlayer : MonoBehaviour
	{
		[Tooltip("Optional. Without an emitter the sound plays with no position.")]
		[SerializeField] private ThunkEmitter m_emitter;

		[FormerlySerializedAs("m_clip")]
		[SerializeField] private ThunkSound m_sound;

		[SerializeField] private bool m_playOnEnable = true;

		[Tooltip("Stops the sound this component last played before playing again.")]
		[SerializeField] private bool m_stopOnPlay;

		[SerializeField] private ThunkPlayerDisableAction m_onDisable;

		private ThunkHandle m_handle;

		public void Play()
		{
			if (m_sound == null) {
				Debug.LogWarning("[Thunk] ThunkPlayer has no sound", this);
				return;
			}

			if (m_stopOnPlay) {
				m_handle.Stop();
			}

			m_handle = m_emitter != null ? m_emitter.Play(m_sound) : m_sound.Play();
		}

		public void Stop()
		{
			m_handle.Stop();
			m_handle = ThunkHandle.None;
		}

		public void Release()
		{
			m_handle.Release();
			m_handle = ThunkHandle.None;
		}

		private void OnEnable()
		{
			if (m_playOnEnable) {
				Play();
			}
		}

		private void OnDisable()
		{
			if (m_onDisable == ThunkPlayerDisableAction.Stop) {
				Stop();
			} else if (m_onDisable == ThunkPlayerDisableAction.Release) {
				Release();
			}
		}
	}
}
