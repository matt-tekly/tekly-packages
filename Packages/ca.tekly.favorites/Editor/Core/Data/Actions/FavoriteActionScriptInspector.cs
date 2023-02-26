using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites
{
	[CustomEditor(typeof(FavoriteActionScriptAsset))]
	public class FavoriteActionScriptEditor : UnityEditor.Editor
	{
		private int m_selectedIndex;
		private string[] m_typeNames;
		private string[] m_assemblyQualifiedNames;
		private SerializedProperty m_assemblyQualifiedName;

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