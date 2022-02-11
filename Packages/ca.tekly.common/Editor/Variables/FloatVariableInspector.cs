using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Variables
{
    [CustomEditor(typeof(FloatVariable), true)]
    public class FloatVariableInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!Application.isPlaying) {
                return;
            }
            
            var variable = target as FloatVariable;
            var current = EditorGUILayout.FloatField("Current", variable.Current);
            
            if (!Mathf.Approximately(current, variable.Current)) {
                variable.Set(current);
            }
        }
    }
}