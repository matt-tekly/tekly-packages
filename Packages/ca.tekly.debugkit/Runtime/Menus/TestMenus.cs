using UnityEngine;

namespace Tekly.DebugKit.Menus
{
	public static class TestMenus
	{
#if DEBUGKIT_DISABLED
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Register()
		{
			DebugKit.Instance.Menu("Test")
				.ButtonRow(row => {
					row.ButtonCopy(() => Debug.Log("Copy"))
						.ButtonPaste(() => Debug.Log("Paste"))
						.ButtonReturn("button-blue", () => Debug.Log("Return"))
						.ButtonTrash("button-red", () => {
							Debug.Log("Trash");
							row.Detach();
						})
						.ButtonUpdate("button-green", () => Debug.Log("Update"));
				});
		}
	}
}