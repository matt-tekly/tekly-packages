using System.Collections.Generic;

namespace Tekly.DevBoard
{
	/// <summary>
	/// View state that should survive a widget being destroyed and rebuilt, like whether a foldout is open, where
	/// a scroll view was scrolled to, or what a list is searched for. Kept in memory for the play session.
	///
	/// Each front end makes its own keys: nodes' views use ViewContext.StateKey.
	/// </summary>
	public class DevBoardState
	{
		private readonly Dictionary<string, object> m_values = new();

		public bool TryGet<T>(string key, out T value)
		{
			if (key != null && m_values.TryGetValue(key, out var stored) && stored is T typed) {
				value = typed;
				return true;
			}

			value = default;
			return false;
		}

		public void Set<T>(string key, T value)
		{
			if (key != null) {
				m_values[key] = value;
			}
		}

		public void Remove(string key)
		{
			if (key != null) {
				m_values.Remove(key);
			}
		}
	}
}
