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
		
		private int m_instanceId;
		private ThunkTrack m_track;
		
		private void Awake()
		{
			m_track = Core.Thunk.Instance.TrackManager.GetTrack(m_trackId);
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
			}
		}
	}
}