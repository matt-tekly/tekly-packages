using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
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
            
            PolymorphicDropdown.Get(fieldInfo).Draw(position, property, (attribute as PolymorphicAttribute).Title);
        }
    }
    
    public class PolymorphicTypeDropdown : AdvancedDropdown
    {
        private readonly string m_title;
        private readonly PolymorphicDropdown m_dropdown;
        private readonly SerializedProperty m_serializedProperty;

        public PolymorphicTypeDropdown(string title, PolymorphicDropdown dropdown, SerializedProperty serializedProperty) 
            : base(new AdvancedDropdownState())
        {
            m_title = title;
            m_dropdown = dropdown;
            m_serializedProperty = serializedProperty;

            minimumSize = new Vector2(minimumSize.x, 45 + Mathf.Clamp(dropdown.TypeNames.Length, 4, 10) * 18f);
        }
        
        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(m_title);

            for (var index = 0; index < m_dropdown.TypeNames.Length; index++) {
                var typeName = m_dropdown.TypeNames[index];
                root.AddChild(new AdvancedDropdownItem(typeName.text) {id = index});
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            m_serializedProperty.serializedObject.UpdateIfRequiredOrScript();
            m_dropdown.Apply(item.id, m_serializedProperty);
            m_serializedProperty.serializedObject.ApplyModifiedProperties();
        }
    }
    
    public class PolymorphicDropdown
    {
        public readonly GUIContent[] TypeNames;
        public readonly Type[] Types;
        
        [NonSerialized]
        private static Dictionary<Type, PolymorphicDropdown> s_data = new Dictionary<Type, PolymorphicDropdown>();
        
        private PolymorphicDropdown(Type type)
        {
            var types = new List<Type> {type};
            types.AddRange(TypeCache.GetTypesDerivedFrom(type));
            Types = types.Where(IsValidType).OrderBy(x => x.Name).ToArray();
            
            var contents = new List<GUIContent> {new GUIContent("null")};
            contents.AddRange(Types.Select(x => new GUIContent(x.Name)));
            
            TypeNames = contents.ToArray();
        }
        
        public void Draw(Rect position, SerializedProperty property, string title = "Types")
        {
            position = EditorGUI.IndentedRect(position);
            if (EditorGUI.DropdownButton(position, GetNameContent(property.managedReferenceValue), FocusType.Keyboard)) {
                var dropDown = new PolymorphicTypeDropdown(title, this, property);
                dropDown.Show(position);
            }
        }

        public void Apply(int index, SerializedProperty property)
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
        
        private GUIContent GetNameContent(object obj)
        {
            if (obj == null) {
                return TypeNames[0];
            }

            var nameIndex = Array.IndexOf(Types, obj.GetType());
            
            if (nameIndex < 0) {
                return new GUIContent("Unknown Type: " + obj.GetType().Name);
            }

            return TypeNames[nameIndex + 1];
        }

        public static PolymorphicDropdown Get(FieldInfo fieldInfo)
        {
            var fieldType = fieldInfo.FieldType;
            
            if (fieldType.IsGenericType) {
                fieldType = fieldType.GetGenericArguments()[0];
            }

            if (fieldType.IsArray) {
                fieldType = fieldType.GetElementType();
            }

            return Get(fieldType);
        }

        public static PolymorphicDropdown Get(Type type)
        {
            if (s_data.TryGetValue(type, out var target)) {
                return target;
            }

            var data = new PolymorphicDropdown(type);
            s_data[type] = data;
            return data;
        }

        private static bool IsValidType(Type type)
        {
            return !type.IsInterface && !type.IsAbstract;
        }
    }
    
    
}