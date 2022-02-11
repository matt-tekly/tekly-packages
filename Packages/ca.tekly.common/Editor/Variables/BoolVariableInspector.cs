using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Variables
{
    [CustomEditor(typeof(BoolVariable), true)]
    public class BoolVariableInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!Application.isPlaying) {
                return;
            }
            
            var variable = target as BoolVariable;
            var current = EditorGUILayout.Toggle("Current", variable.Current);
            
            if (current != variable.Current) {
                variable.Set(current);
            }
        }
    }
}