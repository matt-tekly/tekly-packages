using Tekly.Common.Utils;
using Tekly.Thunk.Core;
using UnityEngine;

namespace Tekly.Thunk.Utils
{
	[RequireComponent(typeof(ThunkEmitter))]
	public class ThunkPlayer : MonoBehaviour
	{
		[SerializeField] private ThunkEmitter m_emitter;
		[SerializeField] private ThunkClip m_clip;
		[SerializeField] private bool m_stopOnPlay;

		private int m_instanceId;
		
		private void OnEnable()
		{
			Play();
		}

		[ContextMenu(nameof(Play))]
		public void Play()
		{
			if (m_stopOnPlay) {
				Stop();
			}
			
			m_instanceId = m_emitter.Play(m_clip);
		}
		
		[ContextMenu(nameof(Stop))]
		public void Stop()
		{
			if (m_instanceId != Core.Thunk.INVALID_ID) {
				m_emitter.Stop(m_instanceId);	
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			this.TryFillComponent(ref m_emitter);
		}
#endif
	}
}