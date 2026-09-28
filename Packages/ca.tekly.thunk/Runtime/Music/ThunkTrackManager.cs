using System;
using System.Collections.Generic;
using Tekly.Thunk.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Thunk.Music
{
	/// <summary>
	/// Settings applied to a track's emitter
	/// </summary>
	[Serializable]
	public struct ThunkTrackSettings
	{
		public string Id;
		
		[Tooltip("Keep playing while AudioListener.pause is true, e.g. pause menu music")]
		public bool IgnoreListenerPause;
		
		[Tooltip("Fade using unscaled time, so fades keep running while timeScale is 0")]
		public bool UseUnscaledDeltaTime;
	}
	
	/// <summary>
	/// Owns the music tracks. Each track has its own emitter so its volume and pause behaviour can be
	/// set separately. Configure tracks in code with ConfigureTrack, or with a ThunkTrackInitializer.
	/// </summary>
	public class ThunkTrackManager
	{
		private readonly Transform m_root;
		private readonly Dictionary<string, ThunkTrack> m_tracks = new Dictionary<string, ThunkTrack>();
		
		public ThunkTrackManager()
		{
			var go = new GameObject("[Thunk] Tracks");
			Object.DontDestroyOnLoad(go);
			
			m_root = go.transform;
		}

		/// <summary>
		/// Gets a track, creating it with default settings if it doesn't exist yet
		/// </summary>
		public ThunkTrack GetTrack(string trackId)
		{
			if (!m_tracks.TryGetValue(trackId, out var track)) {
				var go = new GameObject($"[Thunk] Track {trackId}");
				go.transform.SetParent(m_root, false);
				
				track = new ThunkTrack(go.AddComponent<ThunkEmitter>());
				m_tracks.Add(trackId, track);
			}
			
			return track;
		}

		/// <summary>
		/// Applies settings to a track, creating it if needed. Also applies to anything the track is already playing.
		/// </summary>
		public ThunkTrack ConfigureTrack(ThunkTrackSettings settings)
		{
			var track = GetTrack(settings.Id);
			var emitter = track.Emitter;
			
			emitter.IgnoreListenerPause = settings.IgnoreListenerPause;
			emitter.UseUnscaledDeltaTime = settings.UseUnscaledDeltaTime;
			
			return track;
		}
	}
}