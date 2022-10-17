using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Maths
{
    [CustomPropertyDrawer(typeof(FloatRange))]
    [CustomPropertyDrawer(typeof(IntRange))]
    public class NumericRangePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, label);
            
            var minRect = new Rect(position.x, position.y, position.width * 0.5f - 2.5f, EditorGUIUtility.singleLineHeight);
            var maxRect = new Rect(minRect.xMax + 5, position.y, position.width * 0.5f - 2.5f, EditorGUIUtility.singleLineHeight);
 
            var min = property.FindPropertyRelative("Min");
            var max = property.FindPropertyRelative("Max");

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(minRect, min, GUIContent.none);
            EditorGUI.PropertyField(maxRect, max, GUIContent.none);
            EditorGUI.indentLevel = indent;
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}