using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Gui
{
    public static class PropertyDrawerUtils
    {
        private const float SubLabelSpacing = 4;
		
        public static void DrawMultiplePropertyFields(Rect position, GUIContent[] subLabels, SerializedProperty[] props)
        {
            DrawMultiplePropertyFields(position, SubLabelSpacing, subLabels, props);
        }
		
        public static void DrawMultiplePropertyFields(Rect position, float subLabelSpacing, GUIContent[] subLabels, SerializedProperty[] props)
        {
            var indent = EditorGUI.indentLevel;
            var labelWidth = EditorGUIUtility.labelWidth;
			
            var propsCount = props.Length;
            var width = (position.width - (propsCount - 1) * subLabelSpacing) / propsCount;
            var contentPos = new Rect(position.x, position.y, width, position.height);
			
            EditorGUI.indentLevel = 0;
			
            for (var i = 0; i < propsCount; i++) {
                EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(subLabels[i]).x;
                EditorGUI.PropertyField(contentPos, props[i], subLabels[i]);
                contentPos.x += width + subLabelSpacing;
            }
			
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indent;
        }
    }
}