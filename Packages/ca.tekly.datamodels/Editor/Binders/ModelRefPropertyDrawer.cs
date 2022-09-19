using UnityEditor;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    [CustomPropertyDrawer(typeof(ModelRef))]
    public class ModelRefPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var pathProperty = property.FindPropertyRelative("Path");
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), GetFullKey(pathProperty, label));
            
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            var amountRect = new Rect(position.x, position.y, position.width, position.height);
            
            EditorGUI.PropertyField(amountRect, pathProperty, GUIContent.none);
            
            EditorGUI.EndProperty();
            
            EditorGUI.indentLevel = indentLevel;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private GUIContent GetFullKey(SerializedProperty property, GUIContent label)
        {
            if (string.IsNullOrEmpty(property.stringValue)) {
                return label;
            }
            
            var binder = property.serializedObject.targetObject as Binder;
            
            if (binder != null) {
                var container = binder as BinderContainer;
                if (container != null) {
                    label.tooltip = container.ResolveFullKey();
                } else {
                    label.tooltip = binder.ResolveFullKey(property.stringValue);
                }
            }

            return label;
        }
    }
}