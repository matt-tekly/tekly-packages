using System.Collections.Generic;
using Tekly.Thunk.Core;

namespace Tekly.Thunk.Music
{
	public class ThunkTrack
	{
		private readonly ThunkEmitter m_emitter;
		private readonly List<int> m_musicStack = new List<int>();

		public ThunkTrack(ThunkEmitter emitter)
		{
			m_emitter = emitter;
		}

		/// <summary>
		/// Immediately plays the clip, stopping the active clip
		/// </summary>
		public void Play(ThunkClip clip)
		{
			if (TryPop(out var instanceId)) {
				m_emitter.Stop(instanceId);
			}

			var newInstanceId = m_emitter.Play(clip);
			if (newInstanceId != Core.Thunk.INVALID_ID) {
				m_musicStack.Add(newInstanceId);
			}
		}

		/// <summary>
		/// Crossfades the clip fading out the current clip
		/// </summary>
		public int PlayCrossfade(ThunkClip clip, float crossFadeDuration)
		{
			if (TryPop(out var instanceId) && m_emitter.TryGetInstance(instanceId, out var instance)) {
				instance.FadeOutStop(crossFadeDuration);
			}

			var newInstanceId = m_emitter.Play(clip);

			if (newInstanceId != Core.Thunk.INVALID_ID) {
				m_musicStack.Add(newInstanceId);

				if (m_emitter.TryGetInstance(newInstanceId, out var newInstance)) {
					newInstance.FadeIn(crossFadeDuration);
				}
			}

			return newInstanceId;
		}

		public int PushCrossfade(ThunkClip clip, float crossFadeDuration)
		{
			if (TryPeek(out var instanceId) && m_emitter.TryGetInstance(instanceId, out var instance)) {
				instance.FadeOutPause(crossFadeDuration);
			}

			var newInstanceId = m_emitter.Play(clip);

			if (newInstanceId != Core.Thunk.INVALID_ID) {
				m_musicStack.Add(newInstanceId);

				if (m_emitter.TryGetInstance(newInstanceId, out var newInstance)) {
					newInstance.FadeIn(crossFadeDuration);
				}
			}

			return newInstanceId;
		}

		/// <summary>
		/// Pops the current clip and resumes the previous clip if there is one
		/// </summary>
		public void PopCrossFade(float crossFadeDuration)
		{
			if (TryPop(out var instanceId)) {
				if (m_emitter.TryGetInstance(instanceId, out var instance)) {
					instance.FadeOutStop(crossFadeDuration);
				} else {
					m_emitter.Stop(instanceId);
				}
			}

			if (TryPeek(out var resumingInstanceId)) {
				if (m_emitter.TryGetInstance(resumingInstanceId, out var resumingInstance)) {
					resumingInstance.FadeIn(crossFadeDuration);
				}
			}
		}

		/// <summary>
		/// Pops the music clip with this instanceId.
		/// - If it's on top fades it out and resumes the next.
		/// - If it's not on top removes it from the stack and stops out, but does not change what's currently playing.
		/// </summary>
		public void PopCrossFade(int instanceId, float crossFadeDuration)
		{
			if (!TryPeek(out var topInstanceId)) {
				return;
			}

			if (topInstanceId == instanceId) {
				// Remove from stack (this was missing in your original version)
				TryPop(out _);

				if (m_emitter.TryGetInstance(instanceId, out var instance)) {
					instance.FadeOutStop(crossFadeDuration);
				} else {
					m_emitter.Stop(instanceId);
				}

				if (TryPeek(out var resumingInstanceId)) {
					if (m_emitter.TryGetInstance(resumingInstanceId, out var resumingInstance)) {
						resumingInstance.FadeIn(crossFadeDuration);
					}
				}

				return;
			}

			// Not on top: just remove it if it exists.
			if (RemoveFromStack(instanceId)) {
				if (m_emitter.TryGetInstance(instanceId, out var instance)) {
					instance.Dispose();
				} else {
					m_emitter.Stop(instanceId);
				}
			}
		}

		private bool TryPeek(out int instanceId)
		{
			if (m_musicStack.Count == 0) {
				instanceId = default;
				return false;
			}

			instanceId = m_musicStack[m_musicStack.Count - 1];
			return true;
		}

		private bool TryPop(out int instanceId)
		{
			if (m_musicStack.Count == 0) {
				instanceId = default;
				return false;
			}

			var lastIndex = m_musicStack.Count - 1;
			instanceId = m_musicStack[lastIndex];
			m_musicStack.RemoveAt(lastIndex);
			return true;
		}

		private bool RemoveFromStack(int instanceId)
		{
			for (var i = m_musicStack.Count - 1; i >= 0; i--) {
				if (m_musicStack[i] == instanceId) {
					m_musicStack.RemoveAt(i);
					return true;
				}
			}

			return false;
		}
	}
}
