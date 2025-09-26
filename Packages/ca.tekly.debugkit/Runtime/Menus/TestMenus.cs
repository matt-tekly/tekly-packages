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
			var v = Vector3.zero;
			bool isVisible = true;

			var container = DebugKit.Instance.Menu("Test")
				.CardColumn(card => { card.Checkbox("Visible", () => isVisible, value => isVisible = value); })
				.Row(row => {
					row.Checkbox("", () => isVisible, value => isVisible = value);
					row.ButtonCopy(() => Debug.Log("Copy"))
						.ButtonPaste(() => Debug.Log("Paste"))
						.ButtonReturn("button-blue", () => Debug.Log("Return"))
						.ButtonDownload("button-blue", () => Debug.Log("Download"))
						.ButtonLink("button-blue", () => Debug.Log("Link"))
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
				})
				.Column(col => {
					var text = "Text";
					col.Property("Search", () => text);
					col.SearchField("Search Field", () => text, val => text = val);
					col.SearchField(null, () => text, val => text = val);
					col.TextField("Text Field", () => text, val => text = val);
				});


			var menuController = container.MenuController("asdf");
			menuController.Enable(true);
			var menuA = menuController.Create("a");
			var menuB = menuController.Create("b");

			container.Button("Remove", () => {
				if (menuA != null) {
					menuController.Remove(menuA);
					menuA = null;
				} else if (menuB != null) {
					menuController.Remove(menuB);
					menuB = null;
				}
			});
		}
	}
}