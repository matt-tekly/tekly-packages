using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    [CustomEditor(typeof(BinderContainer), false)]
    public class BinderContainerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            if (GUILayout.Button("Find Children")) {
                FindChildBinders(target as BinderContainer);
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static void FindChildBinders(BinderContainer container)
        {
            Undo.RecordObject(container, "Find Children");
            container.Binders.Clear();

            var binders = container.GetComponents<Binder>().Where(x =>x != container);
            container.Binders.AddRange(binders);
            
            GetChildrenBinders(container.gameObject, container.Binders, new List<Binder>());
        }

        private static void GetChildrenBinders(GameObject gameObject, List<Binder> binders, List<Binder> scratch)
        {
            var transform = gameObject.transform;
            
            for (var index = 0; index < transform.childCount; ++index) {
                var child = transform.GetChild(index);
                var childContainer = child.GetComponent<BinderContainer>();
                
                if (childContainer != null) {
                    if (!childContainer.BindOnEnable) {
                        binders.Add(childContainer);    
                    }
                    continue;
                }
                
                child.GetComponents(scratch);
                binders.AddRange(scratch);
                
                GetChildrenBinders(child.gameObject, binders, scratch);
            }
        }
    }
}