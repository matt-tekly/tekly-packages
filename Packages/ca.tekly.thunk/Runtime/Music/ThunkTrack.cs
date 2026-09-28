using System.Collections.Generic;
using Tekly.Thunk.Core;

namespace Tekly.Thunk.Music
{
	/// <summary>
	/// A stack of music clips on an emitter. Only the top clip is audible; clips underneath are paused and resume
	/// when the clips above them are popped, stopped or finish playing.
	/// </summary>
	public class ThunkTrack
	{
		public ThunkEmitter Emitter => m_emitter;
		
		private readonly ThunkEmitter m_emitter;
		private readonly List<Entry> m_musicStack = new List<Entry>();

		private struct Entry
		{
			public int InstanceId;
			
			/// <summary>
			/// Used to fade the clip underneath back in if this clip finishes or is removed by something other
			/// than this track
			/// </summary>
			public float CrossFadeDuration;

			/// <summary>
			/// Paused (or fading to paused) because another clip was pushed on top of it
			/// </summary>
			public bool Suspended;
		}

		public ThunkTrack(ThunkEmitter emitter)
		{
			m_emitter = emitter;
			m_emitter.InstanceDisposed += OnInstanceDisposed;
		}

		/// <summary>
		/// Immediately plays the clip, stopping the active clip.
		/// If the clip can't play the active clip is left playing.
		/// </summary>
		public int Play(ThunkClip clip)
		{
			var newInstanceId = PlayOverTop(clip, out var hadTop, out var top);

			if (newInstanceId == Core.Thunk.INVALID_ID) {
				return newInstanceId;
			}
			
			if (hadTop && RemoveFromStack(top.InstanceId, out _)) {
				m_emitter.Stop(top.InstanceId);
			}

			Push(newInstanceId, 0);
			return newInstanceId;
		}

		/// <summary>
		/// Crossfades the clip fading out the current clip.
		/// If the clip can't play the active clip is left playing.
		/// </summary>
		public int PlayCrossfade(ThunkClip clip, float crossFadeDuration)
		{
			var newInstanceId = PlayOverTop(clip, out var hadTop, out var top);

			if (newInstanceId == Core.Thunk.INVALID_ID) {
				return newInstanceId;
			}
			
			if (hadTop && RemoveFromStack(top.InstanceId, out _)) {
				FadeOutStop(top.InstanceId, crossFadeDuration);
			}

			PushAndFadeIn(newInstanceId, crossFadeDuration);
			return newInstanceId;
		}

		/// <summary>
		/// Crossfades to the clip, pausing the current clip so it resumes when this one is popped or finishes.
		/// If the clip can't play the active clip is left playing.
		/// </summary>
		public int PushCrossfade(ThunkClip clip, float crossFadeDuration)
		{
			var newInstanceId = PlayOverTop(clip, out var hadTop, out var top);

			if (newInstanceId == Core.Thunk.INVALID_ID) {
				return newInstanceId;
			}
			
			if (hadTop && m_emitter.TryGetInstance(top.InstanceId, out var instance)) {
				instance.FadeOutPause(crossFadeDuration);
			}

			PushAndFadeIn(newInstanceId, crossFadeDuration);
			return newInstanceId;
		}

		/// <summary>
		/// Pops the current clip and resumes the previous clip if there is one
		/// </summary>
		public void PopCrossFade(float crossFadeDuration)
		{
			if (TryPop(out var entry)) {
				FadeOutStop(entry.InstanceId, crossFadeDuration);
			}

			ResumeTop(crossFadeDuration);
		}

		/// <summary>
		/// Pops the music clip with this instanceId.
		/// - If it's on top fades it out and resumes the next.
		/// - If it's not on top removes it from the stack and stops it, but does not change what's currently playing.
		/// </summary>
		public void PopCrossFade(int instanceId, float crossFadeDuration)
		{
			if (IsTop(instanceId)) {
				TryPop(out _);
				FadeOutStop(instanceId, crossFadeDuration);
				ResumeTop(crossFadeDuration);
			} else if (RemoveFromStack(instanceId, out _)) {
				// Not on top: just remove it, without changing what's currently playing
				m_emitter.Stop(instanceId);
			}
		}
		
		/// <summary>
		/// Immediately stops the music clip with this instanceId. If it was on top, the clip underneath resumes
		/// using resumeFadeDuration.
		/// </summary>
		public void Stop(int instanceId, float resumeFadeDuration = 0)
		{
			var wasTop = IsTop(instanceId);
			
			if (RemoveFromStack(instanceId, out _)) {
				m_emitter.Stop(instanceId);

				if (wasTop) {
					ResumeTop(resumeFadeDuration);
				}
			}
		}

		/// <summary>
		/// Handles instances that end without this track removing them: a clip that finished playing, one stopped
		/// directly on the emitter, one replaced due to capacity, or one whose clip was unloaded.
		/// Track methods always remove an entry before stopping its instance, so they never get here for it.
		/// </summary>
		private void OnInstanceDisposed(ThunkClipInstance instance)
		{
			var wasTop = IsTop(instance.Id);
			
			if (!RemoveFromStack(instance.Id, out var entry)) {
				return;
			}

			// Only the audible clip ending resumes the one underneath
			if (wasTop && !entry.Suspended) {
				ResumeTop(entry.CrossFadeDuration);
			}
		}

		/// <summary>
		/// Plays the clip without changing what's audible yet, so the caller only replaces the top clip if this
		/// succeeds. The top is marked Suspended while playing: if the new clip's capacity evicts it, that isn't
		/// treated as the audible clip ending. The mark is cleared again if the clip can't play.
		/// </summary>
		private int PlayOverTop(ThunkClip clip, out bool hadTop, out Entry top)
		{
			hadTop = TryPeek(out top);

			if (hadTop) {
				SetTopSuspended(true);
			}
			
			var newInstanceId = m_emitter.Play(clip);

			if (newInstanceId == Core.Thunk.INVALID_ID && hadTop && IsTop(top.InstanceId)) {
				SetTopSuspended(false);
			}

			return newInstanceId;
		}

		private void PushAndFadeIn(int instanceId, float crossFadeDuration)
		{
			Push(instanceId, crossFadeDuration);

			if (m_emitter.TryGetInstance(instanceId, out var instance)) {
				instance.FadeIn(crossFadeDuration);
			}
		}

		private void FadeOutStop(int instanceId, float crossFadeDuration)
		{
			if (m_emitter.TryGetInstance(instanceId, out var instance)) {
				instance.FadeOutStop(crossFadeDuration);
			}
		}

		/// <summary>
		/// Fades in the top clip, discarding entries whose instance no longer exists
		/// </summary>
		private void ResumeTop(float crossFadeDuration)
		{
			while (TryPeek(out var entry)) {
				if (m_emitter.TryGetInstance(entry.InstanceId, out var instance)) {
					SetTopSuspended(false);
					instance.FadeIn(crossFadeDuration);
					return;
				}

				TryPop(out _);
			}
		}

		private void Push(int instanceId, float crossFadeDuration)
		{
			m_musicStack.Add(new Entry {
				InstanceId = instanceId,
				CrossFadeDuration = crossFadeDuration
			});
		}

		private void SetTopSuspended(bool suspended)
		{
			var lastIndex = m_musicStack.Count - 1;
			var entry = m_musicStack[lastIndex];
			entry.Suspended = suspended;
			m_musicStack[lastIndex] = entry;
		}

		private bool IsTop(int instanceId)
		{
			return TryPeek(out var entry) && entry.InstanceId == instanceId;
		}

		private bool TryPeek(out Entry entry)
		{
			if (m_musicStack.Count == 0) {
				entry = default;
				return false;
			}

			entry = m_musicStack[m_musicStack.Count - 1];
			return true;
		}

		private bool TryPop(out Entry entry)
		{
			if (m_musicStack.Count == 0) {
				entry = default;
				return false;
			}

			var lastIndex = m_musicStack.Count - 1;
			entry = m_musicStack[lastIndex];
			m_musicStack.RemoveAt(lastIndex);
			return true;
		}

		private bool RemoveFromStack(int instanceId, out Entry entry)
		{
			for (var i = m_musicStack.Count - 1; i >= 0; i--) {
				if (m_musicStack[i].InstanceId == instanceId) {
					entry = m_musicStack[i];
					m_musicStack.RemoveAt(i);
					return true;
				}
			}

			entry = default;
			return false;
		}
	}
}
