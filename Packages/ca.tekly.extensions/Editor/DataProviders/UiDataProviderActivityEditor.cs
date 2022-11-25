using System;
using System.Linq;
using Tekly.Common.Gui;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Tekly.Extensions.DataProviders
{
    [CustomEditor(typeof(UiDataProviderActivity))]
    public class UiDataProviderActivityEditor : Editor
    {
        private SerializedProperty m_providers;

        private Type[] m_types;
        private GUIContent[] m_typeNames;
        private int m_selectedType;
        
        private ReorderableList m_reorderableList;
        
        private void OnEnable()
        {
            m_providers = serializedObject.FindProperty("m_providers");
            
            m_types = TypeCache.GetTypesDerivedFrom(typeof(UiDataProvider)).Where(x => !x.IsAbstract).ToArray();
            m_typeNames = m_types.Select(x => new GUIContent(x.Name)).ToArray();
            
            m_reorderableList = new ReorderableList(serializedObject, m_providers, true, true, false, false);
            m_reorderableList.drawElementCallback = DrawListItems;
            m_reorderableList.elementHeightCallback = ElementHeight;
            m_reorderableList.drawHeaderCallback = DrawHeader;
            m_reorderableList.drawFooterCallback = DrawFooter;

            m_reorderableList.footerHeight = EditorGUIUtility.singleLineHeight + 2;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_reorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawListItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= m_providers.arraySize) {
                return;
            }

            var element = m_providers.GetArrayElementAtIndex(index);
            var dropdownRect = rect;
            dropdownRect.xMin = rect.xMax - 20;
            dropdownRect.height = EditorGUIUtility.singleLineHeight;

            if (EditorGUI.DropdownButton(dropdownRect, GUIContent.none, FocusType.Passive)) {
                void HandleItemClicked(object parameter)
                {
                    m_providers.DeleteArrayElementAtIndex((int) parameter);
                    serializedObject.ApplyModifiedProperties();
                    Repaint();
                }

                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove"), false, HandleItemClicked, index);
                menu.DropDown(dropdownRect);
            }

            var elementType = new GUIContent(element.managedReferenceValue.GetType().Name);
            rect.xMin += 8;
            EditorGUI.PropertyField(rect, element, elementType, true);
        }

        private float ElementHeight(int index)
        {
            var element = m_providers.GetArrayElementAtIndex(index);
            if (element.isExpanded) {
                return EditorGUI.GetPropertyHeight(element);
            }

            return EditorGUIUtility.singleLineHeight;
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Providers");
        }

        private void DrawFooter(Rect rect)
        {
            rect.yMin += 2;

            var popupRect = rect;
            popupRect.xMax -= 25;
            m_selectedType = EditorGUI.Popup(popupRect, m_selectedType, m_typeNames);

            var buttonRect = rect;
            buttonRect.xMin = popupRect.xMax + 3;
            buttonRect.height = 18;

            using (EditorGuiExt.BackgroundColorBlock(Color.green)) {
                if (GUI.Button(buttonRect, "+", EditorStyles.miniButton)) {
                    m_providers.arraySize++;
                    var property = m_providers.GetArrayElementAtIndex(m_providers.arraySize - 1);
                    property.managedReferenceValue = Activator.CreateInstance(m_types[m_selectedType]);
                }
            }
        }
    }
}