using System.Collections.Generic;
using System.Text;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Panels;
using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// View state that should survive a widget being destroyed and rebuilt, like whether a foldout is open or
	/// where a scroll view was scrolled to. Kept in memory for the play session.
	///
	/// Keys come from <see cref="KeyFor"/>: a path of the StateScope of each container above the widget, up to
	/// and including its Board or panel, e.g. "main/Game/Economy/Cheats". Rebuilding the same widgets in the same place
	/// produces the same keys.
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

		/// <summary>
		/// Builds the state key for a widget: the scopes of the containers above it, then localKey.
		/// Call it once the widget has been parented.
		/// </summary>
		public static string KeyFor(Component widget, string localKey)
		{
			var builder = new StringBuilder(localKey);

			for (var parent = widget.transform.parent; parent != null; parent = parent.parent) {
				// A panel scopes everything in it by its id, and is as far up as keys go
				if (parent.TryGetComponent(out DevBoardPanel panel)) {
					builder.Insert(0, '/').Insert(0, panel.Id);
					break;
				}

				if (!parent.TryGetComponent(out ContainerWidget container)) {
					continue;
				}

				var scope = container.StateScope;

				if (!string.IsNullOrEmpty(scope)) {
					builder.Insert(0, '/').Insert(0, scope);
				}

				if (container is Board) {
					break;
				}
			}

			return builder.ToString();
		}
	}
}
