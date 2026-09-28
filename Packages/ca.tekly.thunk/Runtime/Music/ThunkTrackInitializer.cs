using UnityEngine;

namespace Tekly.Thunk.Music
{
	/// <summary>
	/// Configures tracks on Awake. Runs before default scripts so ThunkTrackPlayers in the same scene start on
	/// configured tracks; configuring a track that's already playing also applies to what's playing.
	/// </summary>
	[DefaultExecutionOrder(-1000)]
	public class ThunkTrackInitializer : MonoBehaviour
	{
		[SerializeField] private ThunkTrackSettings[] m_tracks;

		private void Awake()
		{
			var trackManager = Core.Thunk.Instance.TrackManager;
			
			for (var index = 0; index < m_tracks.Length; index++) {
				trackManager.ConfigureTrack(m_tracks[index]);
			}
		}
	}
}
