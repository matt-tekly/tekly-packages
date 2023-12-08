using Tekly.Lofi.Core;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace TeklySample.App
{
	public class MusicActivity : InjectableActivity
	{
		[SerializeField] private string m_musicClip;
		[SerializeField] private string m_track;
		[SerializeField] private float m_fadeDuration;
		[SerializeField] private bool m_stopOnLeave;
		
		protected override void ActiveStarted()
		{
			Lofi.Instance.CrossFadeOnTrack(m_musicClip, m_track, m_fadeDuration);
		}

		protected override void InactiveStarted()
		{
			if (m_stopOnLeave) {
				Lofi.Instance.StopTrack(m_track);	
			}
		}
	}
}