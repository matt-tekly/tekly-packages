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
			Vector3 v = Vector3.zero;;
			bool isVisible = true;
			
			DebugKit.Instance.Menu("Test")
				.CardColumn(card => {
					card.Checkbox("Visible", () => isVisible, value => isVisible = value);
				})
				.Row(row => {
					row.Checkbox("", () => isVisible, value => isVisible = value);
					row.ButtonCopy(() => Debug.Log("Copy"))
						.ButtonPaste(() => Debug.Log("Paste"))
						.ButtonReturn("button-blue", () => Debug.Log("Return"))
						.ButtonTrash("button-red", () => {
							Debug.Log("Trash");
							row.Detach();
						})
						.ButtonUpdate("button-green", () => Debug.Log("Update"));
				})
				.Column(card2 => {
					card2.Conditionally(() => isVisible);
					card2.Vector3("Position", () => v, value => v = value);
					card2.Vector3("", () => v, value => v = value);
				});
		}
	}
}