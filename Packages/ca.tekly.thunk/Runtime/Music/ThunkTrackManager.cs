using System.Collections.Generic;
using Tekly.Thunk.Core;
using UnityEngine;

namespace Tekly.Thunk.Music
{
	public class ThunkTrackManager
	{
		private ThunkEmitter m_emitter;
		
		private Dictionary<string, ThunkTrack> m_tracks = new Dictionary<string, ThunkTrack>();
		
		public ThunkTrackManager()
		{
			var go = new GameObject("[Thunk] Track Manager");
			Object.DontDestroyOnLoad(go);
			
			m_emitter = go.AddComponent<ThunkEmitter>();
		}

		public ThunkTrack GetTrack(string trackId)
		{
			if (!m_tracks.TryGetValue(trackId, out var track)) {
				track = new ThunkTrack(m_emitter);
				m_tracks.Add(trackId, track);
			}
			
			return track;
		}
	}
}