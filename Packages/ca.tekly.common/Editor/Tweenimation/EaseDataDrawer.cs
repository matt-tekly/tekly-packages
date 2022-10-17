using Tekly.Common.Tweenimation.Tweens;
using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Tweenimation
{
    [CustomPropertyDrawer(typeof(EaseData))]
    public class EaseDataDrawer : PropertyDrawer
    {
        private readonly string[] m_options = { "Ease", "Curve" };
        
        private GUIStyle m_popupStyle;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_popupStyle == null)
            {
                m_popupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
                m_popupStyle.imagePosition = ImagePosition.ImageOnly;
            }

            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);
            
            EditorGUI.BeginChangeCheck();

            // Get properties
            var useCurve = property.FindPropertyRelative("UseCurve");
            var ease = property.FindPropertyRelative("Ease");
            var curve = property.FindPropertyRelative("Curve");

            // Calculate rect for configuration button
            var buttonRect = new Rect(position);
            buttonRect.yMin += m_popupStyle.margin.top;
            buttonRect.width = m_popupStyle.fixedWidth + m_popupStyle.margin.right;
            
            position.xMin = buttonRect.xMax;
            
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var result = EditorGUI.Popup(buttonRect, useCurve.boolValue ? 1 : 0, m_options, m_popupStyle);

            useCurve.boolValue = result == 1;

            EditorGUI.PropertyField(position, useCurve.boolValue ? curve : ease, GUIContent.none);

            if (EditorGUI.EndChangeCheck()) {
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}