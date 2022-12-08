using Tekly.Common.Gui;
using UnityEditor;
using UnityEngine;

namespace Tekly.PanelViews
{
    [CustomEditor(typeof(PanelView), false)]
    public class PanelViewEditor : Editor
    {
        private PanelView m_panelView;

        private void OnEnable()
        {
            m_panelView = target as PanelView;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
        
            base.OnInspectorGUI();
        
            var container = m_panelView.GetComponentInParent<PanelViewContainer>();

            using (EditorGuiExt.EnabledBlock(false)) {
                using (EditorGuiExt.Horizontal()) {
                    EditorGUILayout.ObjectField("Container", container, typeof(PanelViewContainer), true);
            
                    GUI.enabled = container != null;
            
                    if (GUILayout.Button("Update Container", GUILayout.ExpandWidth(false))) {
                        if (container != null) {
                            container.FindPanels();
                        }
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}