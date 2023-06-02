using UnityEditor;
using UnityEngine;

namespace Tekly.Injectors.Utils
{
	[CustomEditor(typeof(HierarchyInjector), true)]
	public class HierarchyInjectorInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			base.OnInspectorGUI();

			if (GUILayout.Button("Find Children")) {
				FindChildren(target as HierarchyInjector);
			}

			serializedObject.ApplyModifiedProperties();
		}

		private static void FindChildren(HierarchyInjector container)
		{
			Undo.RecordObject(container, "Find Injectable Children");
			container.FindInjectables();
		}
	}
}