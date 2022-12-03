using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Utils
{
    [CustomPropertyDrawer(typeof(PolymorphicAttribute))]
    public class PolymorphicPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? EditorGUI.GetPropertyHeight(property) : EditorGUIUtility.singleLineHeight;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            DrawDropdown(position, property);
            
            EditorGUI.PropertyField(position, property, label, true);
            
            EditorGUI.EndProperty();
        }
        
        private void DrawDropdown(Rect position, SerializedProperty property)
        {
            position.height = EditorStyles.popup.fixedHeight;
            position.yMin += 1;
            position.xMin += EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15f;
            
            PolymorphicPopup.Get(fieldInfo).Draw(position, property);
        }
    }
    
    public class PolymorphicPopup
    {
        public readonly GUIContent[] TypeNames;
        public readonly Type[] Types;
        
        [NonSerialized]
        private static Dictionary<Type, PolymorphicPopup> s_data = new Dictionary<Type, PolymorphicPopup>();
        
        private PolymorphicPopup(Type type)
        {
            var types = new List<Type> {type};
            types.AddRange(TypeCache.GetTypesDerivedFrom(type));
            Types = types.Where(IsValidType).OrderBy(x => x.Name).ToArray();
            
            var contents = new List<GUIContent> {new GUIContent("null")};
            contents.AddRange(Types.Select(x => new GUIContent(x.Name)));
            
            TypeNames = contents.ToArray();
        }
        
        public void Draw(Rect position, SerializedProperty property)
        {
            var currentTypeIndex = GetNameIndex(property.managedReferenceValue);
            currentTypeIndex = EditorGUI.Popup(position, currentTypeIndex, TypeNames);
            Apply(currentTypeIndex, property);
        }

        private void Apply(int index, SerializedProperty property)
        {
            if (index == 0) {
                if (property.managedReferenceValue != null) {
                    property.managedReferenceValue = null;
                }
            } else {
                var selectedType = Types[index - 1];
                
                if (property.managedReferenceValue == null) {
                    property.managedReferenceValue = Activator.CreateInstance(selectedType);
                } else {
                    var currentType = property.managedReferenceValue.GetType();
                    if (currentType != selectedType) {
                        property.managedReferenceValue = Activator.CreateInstance(selectedType);
                    }
                }
            }
        }
        
        private int GetNameIndex(object obj)
        {
            return obj == null ? 0 : Array.IndexOf(Types, obj.GetType()) + 1;
        }

        public static PolymorphicPopup Get(FieldInfo fieldInfo)
        {
            var fieldType = fieldInfo.FieldType;
            
            if (fieldType.IsGenericType) {
                fieldType = fieldType.GetGenericArguments()[0];
            }

            return Get(fieldType);
        }

        public static PolymorphicPopup Get(Type type)
        {
            if (s_data.TryGetValue(type, out var target)) {
                return target;
            }

            var data = new PolymorphicPopup(type);
            s_data[type] = data;
            return data;
        }

        private static bool IsValidType(Type type)
        {
            return !type.IsInterface && !type.IsAbstract;
        }
    }
    
    
}