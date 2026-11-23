using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Thunk
{
	/// <summary>
	/// What a <see cref="ThunkTrack"/> is doing while the game runs. The first entry is the main music, the rest are
	/// interruptions in the order they arrived. Only the last entry is audible: the ones before it are paused, or
	/// have no voice at all and start when their turn comes.
	/// </summary>
	internal sealed class ThunkTrackState
	{
		public int Count => m_entries.Count;

		/// <summary>
		/// The volume the track's music is being held at by ducking, 1 when it is not ducked.
		/// </summary>
		public float DuckGain => m_duckGain;

		private struct Entry
		{
			public ThunkSound Sound;
			public ThunkHandle Handle;
		}

		private struct DuckRequest
		{
			public int Id;
			public float Level;

			/// <summary>
			/// If the request was made by playing a sound over the track, this is that sound's voice. The request lasts
			/// as long as the voice does.
			/// </summary>
			public ThunkHandle Voice;
		}

		private readonly List<Entry> m_entries = new List<Entry>();
		private readonly List<DuckRequest> m_ducks = new List<DuckRequest>();

		private int m_nextDuckId = 1;
		private float m_duckGain = 1f;
		private float m_duckFrom = 1f;
		private float m_duckTo = 1f;
		private float m_duckTime;
		private float m_duckDuration;

		// Copied from the track on every call, so they are still available when a voice ends later
		private float m_crossfade;
		private bool m_resumeHeld;
		private AudioMixerGroup m_mixerGroup;
		private float m_duckLevel;
		private float m_duckFadeDown;
		private float m_duckFadeBack;

		public ThunkSound GetSound(int index)
		{
			return m_entries[index].Sound;
		}

		public ThunkHandle GetHandle(int index)
		{
			return m_entries[index].Handle;
		}

		public ThunkHandle Play(ThunkRuntime runtime, ThunkTrack track, ThunkSound sound, float fade)
		{
			ReadSettings(track);

			if (sound == null) {
				return ThunkHandle.None;
			}

			if (m_entries.Count == 0) {
				var handle = Start(runtime, sound, fade);

				if (!handle.IsNone) {
					m_entries.Add(new Entry { Sound = sound, Handle = handle });
				}

				return handle;
			}

			var main = m_entries[0];

			if (IsSameSound(main.Sound, sound)) {
				return main.Handle;
			}

			if (m_entries.Count > 1) {
				// The track is interrupted, so the new main music waits and starts once it is back on top
				DetachAndStop(runtime, main.Handle, 0f);
				m_entries[0] = new Entry { Sound = sound };

				return ThunkHandle.None;
			}

			var newHandle = Start(runtime, sound, fade);

			// If the new music can't play, the current music is left alone
			if (newHandle.IsNone) {
				return newHandle;
			}

			DetachAndStop(runtime, main.Handle, fade);
			m_entries[0] = new Entry { Sound = sound, Handle = newHandle };

			return newHandle;
		}

		public ThunkHandle Interrupt(ThunkRuntime runtime, ThunkTrack track, ThunkSound sound, float fade)
		{
			ReadSettings(track);

			if (sound == null) {
				return ThunkHandle.None;
			}

			var handle = Start(runtime, sound, fade);

			if (handle.IsNone) {
				return handle;
			}

			if (m_entries.Count > 0) {
				var topIndex = m_entries.Count - 1;
				var top = m_entries[topIndex];

				if (runtime.TryGetVoice(top.Handle, out var voice)) {
					if (m_resumeHeld) {
						runtime.PauseVoice(voice, fade);
					} else {
						voice.Track = null;
						runtime.StopVoice(voice, fade);

						top.Handle = ThunkHandle.None;
						m_entries[topIndex] = top;
					}
				}
			}

			m_entries.Add(new Entry { Sound = sound, Handle = handle });
			return handle;
		}

		public void Stop(ThunkRuntime runtime, float fade)
		{
			for (var index = 0; index < m_entries.Count; index++) {
				DetachAndStop(runtime, m_entries[index].Handle, fade);
			}

			m_entries.Clear();
		}

		public ThunkHandle PlayOver(ThunkRuntime runtime, ThunkTrack track, ThunkSound sound)
		{
			ReadSettings(track);

			// The voice knows about this track so that the duck ends when it does, but it is not one of the track's entries
			var handle = runtime.Play(sound, null, -1f, this);

			if (!handle.IsNone) {
				Duck(runtime, track, m_duckLevel, handle);
			}

			return handle;
		}

		public int Duck(ThunkRuntime runtime, ThunkTrack track, float level, ThunkHandle voice)
		{
			ReadSettings(track);

			var id = m_nextDuckId++;
			m_ducks.Add(new DuckRequest { Id = id, Level = level, Voice = voice });
			RetargetDuck(runtime);

			return id;
		}

		public bool IsDucking(int id)
		{
			for (var index = 0; index < m_ducks.Count; index++) {
				if (m_ducks[index].Id == id) {
					return true;
				}
			}

			return false;
		}

		public void ReleaseDuck(ThunkRuntime runtime, int id)
		{
			for (var index = 0; index < m_ducks.Count; index++) {
				if (m_ducks[index].Id == id) {
					m_ducks.RemoveAt(index);
					RetargetDuck(runtime);
					return;
				}
			}
		}

		/// <summary>
		/// Moves the duck level towards its target. Returns false once it has arrived.
		/// </summary>
		public bool TickDuck(ThunkRuntime runtime, float deltaTime)
		{
			if (m_duckDuration <= 0f) {
				return false;
			}

			m_duckTime += deltaTime;

			var ratio = Mathf.Clamp01(m_duckTime / m_duckDuration);
			m_duckGain = Mathf.Lerp(m_duckFrom, m_duckTo, ratio);
			ApplyDuckGain(runtime);

			if (ratio < 1f) {
				return true;
			}

			m_duckDuration = 0f;
			return false;
		}

		/// <summary>
		/// Called by the runtime when a voice on this track ends or starts fading out to a stop. The fade is how
		/// long what comes back takes to fade in. A negative fade uses the track's crossfade.
		/// </summary>
		public void VoiceEnded(ThunkRuntime runtime, ThunkHandle handle, float fade)
		{
			var index = IndexOf(handle);

			if (index < 0) {
				// This is not music. It is a sound that was played over the track, so its duck ends with it.
				ReleaseDuckOf(runtime, handle);
				return;
			}

			var wasTop = index == m_entries.Count - 1;
			m_entries.RemoveAt(index);

			if (wasTop) {
				BringBackTop(runtime, fade < 0f ? m_crossfade : fade);
			}
		}

		private void BringBackTop(ThunkRuntime runtime, float fade)
		{
			while (m_entries.Count > 0) {
				var top = m_entries[m_entries.Count - 1];

				if (runtime.TryGetVoice(top.Handle, out var voice)) {
					runtime.ResumeVoice(voice, fade);
					return;
				}

				var handle = Start(runtime, top.Sound, fade);

				// Starting a voice can end others, so the top entry is looked up again. An entry waiting to start has
				// no handle, so it can't be one that was removed.
				var topIndex = m_entries.Count - 1;

				if (!handle.IsNone) {
					top.Handle = handle;
					m_entries[topIndex] = top;
					return;
				}

				m_entries.RemoveAt(topIndex);
			}
		}

		private ThunkHandle Start(ThunkRuntime runtime, ThunkSound sound, float fade)
		{
			return runtime.Play(sound, null, fade, this, m_mixerGroup, m_duckGain);
		}

		private void ReleaseDuckOf(ThunkRuntime runtime, ThunkHandle voice)
		{
			for (var index = 0; index < m_ducks.Count; index++) {
				if (m_ducks[index].Voice == voice) {
					m_ducks.RemoveAt(index);
					RetargetDuck(runtime);
					return;
				}
			}
		}

		/// <summary>
		/// The lowest level anyone asked for wins. Requests never multiply together.
		/// </summary>
		private void RetargetDuck(ThunkRuntime runtime)
		{
			var target = 1f;

			for (var index = 0; index < m_ducks.Count; index++) {
				target = Mathf.Min(target, m_ducks[index].Level);
			}

			if (Mathf.Approximately(target, m_duckTo)) {
				return;
			}

			var duration = target < m_duckGain ? m_duckFadeDown : m_duckFadeBack;

			m_duckFrom = m_duckGain;
			m_duckTo = target;
			m_duckTime = 0f;
			m_duckDuration = duration;

			if (duration <= 0f) {
				m_duckGain = target;
				ApplyDuckGain(runtime);
			} else {
				runtime.TickTrack(this);
			}
		}

		private void ApplyDuckGain(ThunkRuntime runtime)
		{
			for (var index = 0; index < m_entries.Count; index++) {
				if (runtime.TryGetVoice(m_entries[index].Handle, out var voice)) {
					voice.TrackGain = m_duckGain;
					voice.Apply();
				}
			}
		}

		private static void DetachAndStop(ThunkRuntime runtime, ThunkHandle handle, float fade)
		{
			if (runtime.TryGetVoice(handle, out var voice)) {
				// Detached first, so that stopping it doesn't call back into this track as a voice that ended
				voice.Track = null;
				runtime.StopVoice(voice, fade);
			}
		}

		private int IndexOf(ThunkHandle handle)
		{
			for (var index = 0; index < m_entries.Count; index++) {
				if (m_entries[index].Handle == handle) {
					return index;
				}
			}

			return -1;
		}

		private void ReadSettings(ThunkTrack track)
		{
			m_crossfade = track.Crossfade;
			m_resumeHeld = track.ResumeHeld;
			m_mixerGroup = track.MixerGroup;
			m_duckLevel = track.DuckLevel;
			m_duckFadeDown = track.DuckFadeDown;
			m_duckFadeBack = track.DuckFadeBack;
		}

		/// <summary>
		/// Two loaded copies of one sound asset are different objects with the same id.
		/// </summary>
		private static bool IsSameSound(ThunkSound a, ThunkSound b)
		{
			if (ReferenceEquals(a, b)) {
				return true;
			}

			return a != null && b != null && a.UniqueId != 0 && a.UniqueId == b.UniqueId;
		}
	}
}
