using UnityEditor;

namespace Tekly.Trellis
{
	[CustomEditor(typeof(LayoutItem))]
	[CanEditMultipleObjects]
	public class LayoutItemEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			TrellisEditorGui.DrawItemProperties(serializedObject);
			serializedObject.ApplyModifiedProperties();
		}
	}
}
