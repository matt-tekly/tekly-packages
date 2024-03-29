using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites
{
	[CustomEditor(typeof(FavoriteActionScriptAsset))]
	public class FavoriteActionScriptEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			if (GUILayout.Button("Test")) {
				var targetPlayer = (FavoriteActionScriptAsset)target;
				targetPlayer.Activate();
			}
		}
	}
}