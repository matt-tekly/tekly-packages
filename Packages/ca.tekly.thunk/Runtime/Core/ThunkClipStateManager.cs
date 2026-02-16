using System;
using System.Collections.Generic;

namespace Tekly.Thunk.Core
{
	public class ThunkClipStateManager : IDisposable
	{
		private readonly Dictionary<ulong, ThunkClipState> m_states = new Dictionary<ulong, ThunkClipState>();

		public void Tick()
		{
			foreach (var thunkClipState in m_states) {
				thunkClipState.Value.Tick();
			}
		}
		
		public ThunkClipState GetOrCreate(ThunkClip clip)
		{
			if (clip == null) {
				return null;
			}

			if (!TryGet(clip, out var state)) {
				state = clip.CreateState();
				m_states[clip.UniqueId] = state;
			}

			return state;
		}

		public bool TryGet(ThunkClip clip, out ThunkClipState state)
		{
			return m_states.TryGetValue(clip.UniqueId, out state);
		}

		public void Unregister(ThunkClip clip)
		{
			m_states.Remove(clip.UniqueId);
		}

		public void Dispose()
		{
			m_states.Clear();
		}
	}
}