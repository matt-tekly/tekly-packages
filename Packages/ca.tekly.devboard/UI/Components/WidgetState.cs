using System.Text;
using Tekly.DevBoard.Panels;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// State keys for widgets built by hand rather than from nodes, from where they are: a path of the StateScope
	/// of each container above the widget, up to and including its panel, e.g. "main/Game/Economy/Cheats".
	/// Rebuilding the same widgets in the same place produces the same keys.
	/// </summary>
	public static class WidgetState
	{
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
			}

			return builder.ToString();
		}
	}
}
