using System;
using System.Linq;
using Tekly.Common.Gui;
using Tekly.Common.Maths;
using Tekly.Common.Tweenimation.Tweens;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.Common.Tweenimation
{
    [CustomEditor(typeof(Tweenimator))]
    public class TweenimatorEditor : Editor
    {
        private SerializedProperty m_tweens;
        private SerializedProperty m_delay;
        private SerializedProperty m_playOnEnable;
        private SerializedProperty m_completedEvent;

        private Type[] m_tweenTypes;
        private GUIContent[] m_tweenTypeNames;
        private int m_selectedType;

        private bool m_enableTimeline;
        private float m_timeline;
        private Tweenimator m_tweenimator;
        private ReorderableList m_reorderableList;

        private static bool m_eventsExpanded;

        private void OnEnable()
        {
            m_tweens = serializedObject.FindProperty("m_tweens");
            m_delay = serializedObject.FindProperty("m_delay");
            m_playOnEnable = serializedObject.FindProperty("m_playOnEnable");
            m_completedEvent = serializedObject.FindProperty("m_completed");

            m_tweenTypes = TypeCache.GetTypesDerivedFrom(typeof(BaseTween)).Where(x => !x.IsAbstract).ToArray();
            m_tweenTypeNames = m_tweenTypes.Select(x => new GUIContent(x.Name)).ToArray();

            m_tweenimator = serializedObject.targetObject as Tweenimator;

            m_reorderableList = new ReorderableList(serializedObject, m_tweens, true, true, false, false);
            m_reorderableList.drawElementCallback = DrawListItems;
            m_reorderableList.elementHeightCallback = ElementHeight;
            m_reorderableList.drawHeaderCallback = DrawHeader;
            m_reorderableList.drawFooterCallback = DrawFooter;
            
            m_reorderableList.footerHeight = EditorGUIUtility.singleLineHeight + 2;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(m_playOnEnable);

            m_reorderableList.DoLayoutList();

            if (Application.isPlaying) {
                using (EditorGuiExt.SmallContainer(Color.yellow, true)) {
                    m_enableTimeline = EditorGUILayout.ToggleLeft("Timeline", m_enableTimeline, GUILayout.Width(80));
                    m_timeline = EditorGUILayout.Slider(m_timeline, 0, m_tweenimator.AnimationDuration);

                    if (m_enableTimeline) {
                        m_tweenimator.Evaluate(m_timeline);
                    }
                }
            }

            m_eventsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_eventsExpanded, "Events");
            
            if (m_eventsExpanded) {
                EditorGUILayout.PropertyField(m_completedEvent);    
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawListItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= m_tweens.arraySize) {
                return;
            }
            
            var element = m_tweens.GetArrayElementAtIndex(index);
            var enabled = element.FindPropertyRelative("m_enabled");

            var toggleRect = rect;
            toggleRect.height = EditorGUIUtility.singleLineHeight;
            toggleRect.xMin = rect.xMax - 38;
            toggleRect.width = 18;
            enabled.boolValue = EditorGUI.Toggle(toggleRect, enabled.boolValue);

            var dropdownRect = rect;
            dropdownRect.xMin = rect.xMax - 20;
            dropdownRect.height = EditorGUIUtility.singleLineHeight;
            if (EditorGUI.DropdownButton(dropdownRect, GUIContent.none, FocusType.Passive))
            {
                void HandleItemClicked(object parameter)
                {
                    m_tweens.DeleteArrayElementAtIndex((int) parameter);
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
            var element = m_tweens.GetArrayElementAtIndex(index);
            if (element.isExpanded) {
                return EditorGUI.GetPropertyHeight(element);
            }

            return EditorGUIUtility.singleLineHeight;
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Tweens");

            var delayRect = rect;
            delayRect.xMin = rect.xMax - 88;
            EditorGUI.LabelField(delayRect, "Delay");
            
            rect.xMin = rect.xMax - 50;
            rect = rect.Shrink(1);
            
            EditorGUI.BeginProperty(rect, GUIContent.none, m_delay);
            m_delay.floatValue = EditorGUI.FloatField(rect, m_delay.floatValue, EditorStyles.toolbarTextField);
            EditorGUI.EndProperty();
        }

        private void DrawFooter(Rect rect)
        {
            rect.yMin += 2;

            var popupRect = rect;
            popupRect.xMax -= 25;
            m_selectedType = EditorGUI.Popup(popupRect, m_selectedType, m_tweenTypeNames);

            var buttonRect = rect;
            buttonRect.xMin = popupRect.xMax + 3;
            buttonRect.height = 18;

            using (EditorGuiExt.BackgroundColorBlock(Color.green)) {
                if (GUI.Button(buttonRect, "+", EditorStyles.miniButton)) {
                    m_tweens.arraySize++;
                    var property = m_tweens.GetArrayElementAtIndex(m_tweens.arraySize - 1);
                    property.managedReferenceValue = Activator.CreateInstance(m_tweenTypes[m_selectedType]);
                }
            }
        }
    }
}