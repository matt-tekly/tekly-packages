using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.DebugKit.Menus
{
	public static class UIMenu
	{
#if DEBUGKIT_DISABLED
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Register()
		{
			DebugKit.Instance.Menu("UI", menu => {
				menu.Property("Selection", GetSelection);
				menu.Row(row => {
					row.Label("Canvases:");
					row.FlexibleSpace();
					row.OnOffButtons(SetCanvasesEnabled);
				});
			});
		}

		private static void SetCanvasesEnabled(bool enabled)
		{
			var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

			foreach (var canvas in canvases) {
				canvas.enabled = enabled;
			}
		}

		private static string GetSelection()
		{
			if (EventSystem.current == null) {
				return "No Event System";
			}

			if (EventSystem.current.currentSelectedGameObject == null) {
				return "No Selection";
			}

			return EventSystem.current.currentSelectedGameObject.name;
		}
	}
}