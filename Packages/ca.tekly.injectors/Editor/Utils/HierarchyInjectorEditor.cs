using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tekly.Injectors.Utils
{
	[CustomEditor(typeof(HierarchyInjector), false)]
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

		public static void FindChildren(HierarchyInjector container)
		{
			Undo.RecordObject(container, "Find Children");
			container.Behaviours.Clear();
			GetChildren(container.gameObject, container.Behaviours, new List<InjectableBehaviour>());
		}

		private static void GetChildren(GameObject gameObject, List<InjectableBehaviour> binders, List<InjectableBehaviour> scratch)
		{
			var transform = gameObject.transform;
            
			for (var index = 0; index < transform.childCount; ++index) {
				var child = transform.GetChild(index);
				var childContainer = child.GetComponent<HierarchyInjector>();
                
				if (childContainer != null) {
					continue;
				}
                
				child.GetComponents(scratch);
				binders.AddRange(scratch);
                
				GetChildren(child.gameObject, binders, scratch);
			}
		}
	}
}