using System;
using System.Collections.Generic;

namespace Tekly.Thunk.Core
{
	public class ThunkClipStateManager : IDisposable
	{
		private readonly Dictionary<ulong, ThunkClipState> m_states = new Dictionary<ulong, ThunkClipState>();

		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			foreach (var thunkClipState in m_states) {
				thunkClipState.Value.Tick(deltaTime, unscaledDeltaTime);
			}
		}
		
		/// <summary>
		/// Gets the shared state for a clip, creating it if needed, and registers this copy of the clip with it.
		/// </summary>
		public ThunkClipState GetOrCreate(ThunkClip clip)
		{
			if (clip == null) {
				return null;
			}

			if (TryGet(clip, out var state)) {
				state.AddClip(clip);
			} else {
				state = clip.CreateState();
				m_states[clip.UniqueId] = state;
			}

			return state;
		}

		public bool TryGet(ThunkClip clip, out ThunkClipState state)
		{
			return m_states.TryGetValue(clip.UniqueId, out state);
		}

		/// <summary>
		/// Called when a copy of a clip is unloaded. The state is only removed when no loaded copies remain,
		/// at which point any instances still playing are disposed so their AudioSources return to the pool.
		/// </summary>
		public void Unregister(ThunkClip clip)
		{
			if (!TryGet(clip, out var state)) {
				return;
			}

			// A copy that never registered (e.g. never played) must not remove the shared state
			if (!state.RemoveClip(clip) || state.HasClips) {
				return;
			}

			m_states.Remove(clip.UniqueId);
			state.DisposeInstances();
		}

		public void Dispose()
		{
			m_states.Clear();
		}
	}
}