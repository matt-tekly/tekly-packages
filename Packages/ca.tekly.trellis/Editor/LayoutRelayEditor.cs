using UnityEditor;

namespace Tekly.Trellis
{
	[CustomEditor(typeof(LayoutRelay))]
	[CanEditMultipleObjects]
	public class LayoutRelayEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox("Connects the layouts above and below this object so they rebuild together. " +
				"It doesn't move or size anything.", MessageType.None);
		}
	}
}
