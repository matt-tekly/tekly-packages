using Tekly.Common.Gui;
using UnityEditor;
using UnityEditor.SceneManagement;
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
                if (!EditorSceneManager.IsPreviewSceneObject(target) && !PrefabUtility.IsPartOfAnyPrefab(target)) {
                    return;
                }
            }

            DrawBinderContainer();
        }

        protected void DrawBinderContainer()
        {
            using (EditorGuiExt.EnabledBlock(false)) {
                using (EditorGuiExt.Horizontal()) {
                    var binder = target as Binder;
                    var container = binder.GetComponentInParent<BinderContainer>();
                    
                    EditorGUILayout.ObjectField("Container", container, typeof(BinderContainer), true);
            
                    GUI.enabled = container != null;
            
                    if (GUILayout.Button("Update Container", GUILayout.ExpandWidth(false))) {
                        if (container != null) {
                            BinderContainerInspector.FindChildBinders(container);    
                        }
                    }
                }
            }
        }
    }
}