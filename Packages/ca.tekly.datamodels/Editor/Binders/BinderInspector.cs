using Tekly.Common;
using Tekly.Common.Gui;
using UnityEditor;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    [CustomEditor(typeof(Binder), true)]
    public class BinderInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            if (EditorApplication.isPlaying) {
                return;
            }
            
            var binder = target as Binder;
            
            var container = binder.GetComponentInParent<BinderContainer>();

            using (EditorGuiExt.EnabledBlock(false)) {
                using (EditorGuiExt.Horizontal()) {
                    EditorGUILayout.ObjectField("Container", container, typeof(BinderContainer), true);
            
                    GUI.enabled = container != null;
            
                    if (GUILayout.Button("Update Container", GUILayout.ExpandWidth(false))) {
                        if (container != null) {
                            BinderContainerInspector.FindChildBinders(container);    
                        }
                    }
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}