using System.Collections.Generic;
using Tekly.Common.Utils;

namespace Tekly.Thunk
{
	/// <summary>
	/// Finds the runtime state that belongs to an asset. Assets are keyed by their UniqueId, so every loaded copy of
	/// an asset (the same asset pulled into several bundles) shares one state. An asset created at runtime has no
	/// id, so it is keyed by the object itself.
	/// </summary>
	internal sealed class ThunkStateMap<TState> where TState : class, new()
	{
		private readonly Dictionary<ulong, TState> m_byId = new Dictionary<ulong, TState>();
		private readonly Dictionary<UniqueIdScriptableObject, TState> m_byObject = new Dictionary<UniqueIdScriptableObject, TState>();

		public TState Get(UniqueIdScriptableObject asset)
		{
			TState state;
			var id = asset.UniqueId;

			if (id != 0) {
				if (!m_byId.TryGetValue(id, out state)) {
					state = new TState();
					m_byId.Add(id, state);
				}

				return state;
			}

			if (!m_byObject.TryGetValue(asset, out state)) {
				state = new TState();
				m_byObject.Add(asset, state);
			}

			return state;
		}

		public void Clear()
		{
			m_byId.Clear();
			m_byObject.Clear();
		}
	}
}
