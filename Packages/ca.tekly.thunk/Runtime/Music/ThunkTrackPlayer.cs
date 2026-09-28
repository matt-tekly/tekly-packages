using Tekly.Thunk.Core;
using UnityEngine;

namespace Tekly.Thunk.Music
{
	public class ThunkTrackPlayer : MonoBehaviour
	{
		[SerializeField] private ThunkClip m_clip;
		[SerializeField] private string m_trackId;
		[SerializeField] private bool m_push;
		[SerializeField] private float m_fadeInDuration;
		[SerializeField] private bool m_autoPlay;
		
		private int m_instanceId = Core.Thunk.INVALID_ID;
		private ThunkTrack m_track;
		
		private void Awake()
		{
			m_track = Core.Thunk.Instance.TrackManager.GetTrack(m_trackId);
		}

		private void OnEnable()
		{
			if (m_autoPlay) {
				Play();
			}
		}

		[ContextMenu(nameof(Play))]
		public void Play()
		{
			if (m_push) {
				m_instanceId = m_track.PushCrossfade(m_clip, m_fadeInDuration);	
			} else {
				m_instanceId = m_track.PlayCrossfade(m_clip, m_fadeInDuration);
			}
		}
		
		[ContextMenu(nameof(Pop))]
		public void Pop()
		{
			if (m_instanceId != Core.Thunk.INVALID_ID) {
				m_track.PopCrossFade(m_instanceId, m_fadeInDuration);
				m_instanceId = Core.Thunk.INVALID_ID;
			}
		}

		private void OnDisable()
		{
			if (m_instanceId != Core.Thunk.INVALID_ID) {
				// Resumes whatever was underneath if this was on top
				m_track.Stop(m_instanceId, m_fadeInDuration);
				m_instanceId = Core.Thunk.INVALID_ID;
			}
		}
	}
}