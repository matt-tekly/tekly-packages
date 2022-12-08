using UnityEditor;
using UnityEngine;

namespace Tekly.PanelViews
{
    [CustomEditor(typeof(PanelViewContainer), false)]
    public class PanelViewContainerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
        
            base.OnInspectorGUI();
        
            if (GUILayout.Button("Find Children")) {
                FindChildBinders(target as PanelViewContainer);
            }
        
            serializedObject.ApplyModifiedProperties();
        }

        private static void FindChildBinders(PanelViewContainer container)
        {
            Undo.RecordObject(container, "Find Children");
            container.FindPanels();
        }
    }
}