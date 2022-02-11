using System;
using UnityEditor;
using UnityEngine;

namespace Tekly.Common
{
    public readonly struct GuiLayoutTracker : IDisposable
    {
        private readonly bool m_horizontal;
        
        public GuiLayoutTracker(bool horizontal, Color color, GUIStyle guiStyle)
        {
            m_horizontal = horizontal;
            
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            Apply(guiStyle);
            GUI.backgroundColor = prevColor;
        }

        public GuiLayoutTracker(bool horizontal, GUIStyle guiStyle)
        {
            m_horizontal = horizontal;
            Apply(guiStyle);
        }

        private void Apply(GUIStyle guiStyle)
        {
            if (m_horizontal) {
                EditorGUILayout.BeginHorizontal(guiStyle);    
            } else {
                EditorGUILayout.BeginVertical(guiStyle);
            }
        }
        
        public void Dispose()
        {
            if (m_horizontal) {
                EditorGUILayout.EndHorizontal();    
            } else {
                EditorGUILayout.EndVertical();
            }
        }
    }
}