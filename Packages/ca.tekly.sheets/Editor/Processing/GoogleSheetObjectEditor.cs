using UnityEditor;
using UnityEngine;

namespace Tekly.Sheets.Processing
{
	[CustomEditor(typeof(GoogleSheetObject), false)]
	public class GoogleSheetObjectEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			base.OnInspectorGUI();

			if (GUILayout.Button("Open Sheet")) {
				var googleSheetObject = target as GoogleSheetObject;
				googleSheetObject.OpenSheet();
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}