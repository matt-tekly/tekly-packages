using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.Thunk
{
	public enum ThunkTrackPlayMode
	{
		/// <summary>Becomes the track's main music. Keeps playing when this component is disabled.</summary>
		Main,

		/// <summary>Holds what the track is playing until this component is disabled or the sound ends.</summary>
		Interrupt,

		/// <summary>Plays over the track's music, which is ducked until this component is disabled or the sound ends.</summary>
		Over
	}

	/// <summary>
	/// Plays a sound on a track without code.
	/// </summary>
	public class ThunkTrackPlayer : MonoBehaviour
	{
		[SerializeField] private ThunkTrack m_track;

		[FormerlySerializedAs("m_clip")]
		[SerializeField] private ThunkSound m_sound;

		[FormerlySerializedAs("m_push")]
		[SerializeField] private ThunkTrackPlayMode m_mode;

		[FormerlySerializedAs("m_fadeInDuration")]
		[Tooltip("Seconds to fade when starting and stopping.")]
		[Min(0f)]
		[SerializeField] private float m_fade;

		[FormerlySerializedAs("m_autoPlay")]
		[SerializeField] private bool m_playOnEnable;

		private ThunkHandle m_handle;

		[ContextMenu(nameof(Play))]
		public void Play()
		{
			if (m_track == null) {
				Debug.LogWarning("[Thunk] ThunkTrackPlayer has no track", this);
				return;
			}

			if (m_mode == ThunkTrackPlayMode.Interrupt) {
				// Playing again replaces this component's own interruption, it doesn't stack another
				m_handle.Stop(m_fade);
				m_handle = m_track.Interrupt(m_sound, m_fade);
			} else if (m_mode == ThunkTrackPlayMode.Over) {
				m_handle.Stop(m_fade);
				m_handle = m_track.PlayOver(m_sound);
			} else {
				m_handle = m_track.Play(m_sound, m_fade);
			}
		}

		[ContextMenu(nameof(Stop))]
		public void Stop()
		{
			m_handle.Stop(m_fade);
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
			if (m_mode != ThunkTrackPlayMode.Main) {
				Stop();
			}
		}
	}
}
