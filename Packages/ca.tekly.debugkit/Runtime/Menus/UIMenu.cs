using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.DebugKit.Menus
{
	public static class UIMenu
	{
		private static bool s_autoRefresh = true;
		private static string s_selection;
		private static int s_canvases;
		
#if DEBUGKIT_DISABLED
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Register()
		{
			DebugKit.Instance.Menu("UI")
				.Updater(UpdateStats, () => s_autoRefresh)
				.Property("Selection", () => s_selection)
				.Column("raised p4 r4 mv4", column => {
					column.Row("spaced", row => {
						row.Label("Stats");
						row.Button("Refresh", UpdateStats);
					});

					column.Property("Canvases", () => s_canvases);
					
				}).Checkbox("Auto Refresh", () => s_autoRefresh, v => s_autoRefresh = v);
		}

		private static void UpdateStats()
		{
			s_selection = GetSelection();
			s_canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
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