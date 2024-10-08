// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.EditorUtils.Gui;
using Tekly.TreeState.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Tekly.TreeState
{
	[CustomEditor(typeof(TreeState), false)]
	public class TreeStateInspector : Editor
	{
		private const float RectHeight = 18;
		private const float Padding = 2;

		protected SerializedProperty LoadType;
		
		private SerializedProperty m_transitions;
		private ReorderableList m_transitionList;

		private TreeState m_state;

		private string m_transition;
		private readonly List<string> m_validTransitions = new List<string>();

		protected virtual void OnEnable()
		{
			m_state = serializedObject.targetObject as TreeState;

			LoadType = serializedObject.FindProperty("LoadType");
			m_transitions = serializedObject.FindProperty("Transitions");
			m_transitionList = new ReorderableList(serializedObject, m_transitions, true, false, true, true);

			m_transitionList.drawElementCallback = DrawElement;
			m_transitionList.drawHeaderCallback = DrawHeader;
		}

		private void DrawHeader(Rect rect)
		{
			EditorGUI.LabelField(rect, "Transitions");
		}

		private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			rect.yMin += Padding;
			rect.height = RectHeight;

			var transition = m_transitions.GetArrayElementAtIndex(index);
			var transitionName = transition.FindPropertyRelative("TransitionName");
			var transitionTarget = transition.FindPropertyRelative("Target");

			var transitionNameRect = rect;
			transitionNameRect.width = rect.width * 0.45f - 2f;

			var targetRect = rect;
			targetRect.xMin += rect.width * 0.45f + 2f;

			EditorGUI.PropertyField(transitionNameRect, transitionName, GUIContent.none, false);
			EditorGUI.PropertyField(targetRect, transitionTarget, GUIContent.none, false);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawProperties();

			DrawTransitions();

			DrawDebug();

			serializedObject.ApplyModifiedProperties();
		}

		protected virtual void DrawProperties()
		{
			EditorGUILayout.PropertyField(LoadType);
		}
		
		private void DrawTransitions()
		{
			using (EditorGuiExt.EnabledBlock(!EditorApplication.isPlaying)) {
				m_transitionList.DoLayoutList();	
			}
		}

		private void DrawDebug()
		{
			if (!EditorApplication.isPlaying) {
				return;
			}
			
			if (EditorSceneManager.IsPreviewSceneObject(target) || PrefabUtility.IsPartOfAnyPrefab(target)) {
				return;
			}

			using (EditorGuiExt.ColorBlock(Color.green)) {
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);	
			}

			GUILayout.Label("Debug", EditorStyles.boldLabel);
			
			using (EditorGuiExt.EnabledBlock(false)) {
				EditorGUILayout.ObjectField("Active State", m_state.Manager.Active, typeof(TreeState), true);
				EditorGUILayout.EnumPopup("Mode", m_state.Mode);	
			}
			
			m_validTransitions.Clear();
			TreeStateUtils.GetValidTransitions(m_state.Manager.Active, m_validTransitions);
			
			if (m_validTransitions.Count > 0) {
				EditorGUILayout.BeginHorizontal();
				
				var content = m_validTransitions.Select(x => new GUIContent(x)).ToArray();
				var index = Math.Clamp(m_validTransitions.IndexOf(m_transition), 0, m_validTransitions.Count-1);
				index = EditorGUILayout.Popup(new GUIContent("Transition"), index, content);
				
				m_transition = m_validTransitions[index];
			
				if (GUILayout.Button("Go", GUILayout.Width(30))) {
					m_state.HandleTransition(m_transition);
				}
				
				EditorGUILayout.EndHorizontal();
			} else {
				GUILayout.Label("No Valid Transitions");
			}

			if (GUILayout.Button("Transition To This")) {
				m_state.TransitionTo(m_state);
			}


			EditorGUILayout.EndVertical();
		}
	}
}