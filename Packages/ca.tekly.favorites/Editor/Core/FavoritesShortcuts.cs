using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites
{
	public static class FavoritesShortcuts
	{
		[MenuItem("Tools/Favorites %G")]
		private static void ShowWindow()
		{
			var currentEvent = Event.current;

			var hotkeyUsed = currentEvent?.isKey == true && currentEvent.control && currentEvent.keyCode == KeyCode.G;

			if (hotkeyUsed) {
				FavoritesWindow.Present();
			} else {
				EditorWindow.GetWindow<FavoritesWindow>();
			}
		}
	}
}