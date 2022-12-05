using UnityEditor;
using UnityEngine;

namespace Tekly.Injectors.Utils
{
    [CustomPropertyDrawer(typeof(RegisteredComponent))]
    public class RegisteredComponentDrawer : PropertyDrawer
    {
        private readonly GUIContent[] m_popupOptions = {new GUIContent("No Id"), new GUIContent("Use Id")};
        private GUIStyle m_popupStyle;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_popupStyle == null) {
                m_popupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
                m_popupStyle.imagePosition = ImagePosition.ImageOnly;
            }

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            
            var useId = property.FindPropertyRelative("UseId");
            var idValue = property.FindPropertyRelative("Id");
            var component = property.FindPropertyRelative("Component");

            // Calculate rect for configuration button
            var buttonRect = new Rect(position);
            buttonRect.yMin += m_popupStyle.margin.top;
            buttonRect.width = m_popupStyle.fixedWidth + m_popupStyle.margin.right;
            position.xMin = buttonRect.xMax;

            var result = EditorGUI.Popup(buttonRect, useId.boolValue ? 1 : 0, m_popupOptions, m_popupStyle);

            useId.boolValue = result == 1;

            if (useId.boolValue) {
                position.height = EditorGUIUtility.singleLineHeight;
                var idRect = position;
                idRect.width *= .4f;
                EditorGUI.PropertyField(idRect, idValue, GUIContent.none);

                var componentRect = position;
                componentRect.xMin += idRect.width + 5;
                EditorGUI.PropertyField(componentRect, component, GUIContent.none);
            } else {
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, component, GUIContent.none);
            }

            if (EditorGUI.EndChangeCheck()) {
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }
    }
}