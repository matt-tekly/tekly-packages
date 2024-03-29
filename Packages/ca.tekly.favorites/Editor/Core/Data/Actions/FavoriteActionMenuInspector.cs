using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites
{
	[CustomEditor(typeof(FavoriteActionMenu))]
	public class FavoriteActionMenuEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			if (GUILayout.Button("Test")) {
				var targetPlayer = (FavoriteActionMenu)target;
				targetPlayer.Activate();
			}
		}
	}
}