using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// Draws clip sets as reorderable lists. Each row has a button that plays that one clip, the clip and its
	/// length. The selection mode sits in the header and is hidden while there is nothing to choose between.
	/// Several clips can be dropped on a list at once. An inspector keeps one of these, and it holds a list for
	/// every clip set that inspector has drawn.
	/// </summary>
	internal sealed class ThunkClipSetLists
	{
		private const float PLAY_WIDTH = 24f;
		private const float LENGTH_WIDTH = 48f;
		private const float MODE_WIDTH = 120f;
		private const float SPACING = 4f;

		private static GUIContent s_playContent;
		private static GUIStyle s_lengthStyle;

		private readonly Dictionary<string, ReorderableList> m_lists = new Dictionary<string, ReorderableList>();
		private readonly Action<AudioClip> m_play;

		public ThunkClipSetLists(Action<AudioClip> play)
		{
			m_play = play;
		}

		public void Draw(SerializedProperty set, GUIContent label)
		{
			var clips = set.FindPropertyRelative("Clips");
			var list = GetList(set, clips, label);

			var rect = GUILayoutUtility.GetRect(0f, list.GetHeight(), GUILayout.ExpandWidth(true));
			rect = EditorGUI.IndentedRect(rect);

			// Before the list, so the clip fields in its rows don't take a drop that is meant for the whole list
			HandleDrop(rect, list.headerHeight, clips);

			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			list.DoList(rect);
			EditorGUI.indentLevel = indent;
		}

		private ReorderableList GetList(SerializedProperty set, SerializedProperty clips, GUIContent label)
		{
			if (m_lists.TryGetValue(set.propertyPath, out var list)) {
				list.serializedProperty = clips;
				return list;
			}

			// The callbacks look the set up by its path, as the property passed in is only good for this call
			var serializedObject = set.serializedObject;
			var setPath = set.propertyPath;
			var header = new GUIContent(label);

			list = new ReorderableList(serializedObject, clips, true, true, true, true);
			list.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
			list.drawHeaderCallback = rect => DrawHeader(rect, serializedObject.FindProperty(setPath), header);
			list.drawElementCallback = (rect, index, active, focused) => DrawElement(rect, serializedObject.FindProperty(setPath), index);
			list.onAddCallback = added => {
				// Growing the array copies its last element, and a new row should start empty
				var property = added.serializedProperty;
				var index = property.arraySize;

				property.arraySize++;
				property.GetArrayElementAtIndex(index).objectReferenceValue = null;
				added.index = index;
			};

			m_lists.Add(setPath, list);
			return list;
		}

		private static void DrawHeader(Rect rect, SerializedProperty set, GUIContent label)
		{
			if (set == null) {
				return;
			}

			var labelRect = rect;

			if (set.FindPropertyRelative("Clips").arraySize > 1) {
				var modeRect = new Rect(rect.xMax - MODE_WIDTH, rect.y + 1f, MODE_WIDTH, rect.height - 2f);
				labelRect.xMax = modeRect.x - SPACING;

				EditorGUI.PropertyField(modeRect, set.FindPropertyRelative("Mode"), GUIContent.none);
			}

			EditorGUI.LabelField(labelRect, label);
		}

		private void DrawElement(Rect rect, SerializedProperty set, int index)
		{
			if (set == null) {
				return;
			}

			var clips = set.FindPropertyRelative("Clips");

			if (index >= clips.arraySize) {
				return;
			}

			EnsureStyles();

			var element = clips.GetArrayElementAtIndex(index);
			var clip = element.objectReferenceValue as AudioClip;

			rect.y += 2f;
			rect.height = EditorGUIUtility.singleLineHeight;

			var playRect = new Rect(rect.x, rect.y, PLAY_WIDTH, rect.height);
			var lengthRect = new Rect(rect.xMax - LENGTH_WIDTH, rect.y, LENGTH_WIDTH, rect.height);
			var fieldRect = new Rect(playRect.xMax + SPACING, rect.y, lengthRect.x - playRect.xMax - SPACING * 2f, rect.height);

			using (new EditorGUI.DisabledScope(clip == null)) {
				if (GUI.Button(playRect, s_playContent, EditorStyles.miniButton)) {
					m_play?.Invoke(clip);
				}
			}

			EditorGUI.PropertyField(fieldRect, element, GUIContent.none);

			if (clip != null) {
				EditorGUI.LabelField(lengthRect, clip.length.ToString("0.00") + "s", s_lengthStyle);
			}
		}

		/// <summary>
		/// Adds every dragged AudioClip to the set. A single clip dragged onto a row is left for that row's field,
		/// which replaces the clip in it.
		/// </summary>
		private static void HandleDrop(Rect rect, float headerHeight, SerializedProperty clips)
		{
			var current = Event.current;

			if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform) {
				return;
			}

			if (!rect.Contains(current.mousePosition)) {
				return;
			}

			var dragged = DragAndDrop.objectReferences;
			var count = 0;

			for (var i = 0; i < dragged.Length; i++) {
				if (dragged[i] is AudioClip) {
					count++;
				}
			}

			var overHeader = current.mousePosition.y <= rect.y + headerHeight;

			if (count == 0 || (count == 1 && !overHeader)) {
				return;
			}

			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

			if (current.type == EventType.DragPerform) {
				DragAndDrop.AcceptDrag();

				for (var i = 0; i < dragged.Length; i++) {
					if (dragged[i] is AudioClip clip) {
						var index = clips.arraySize;

						clips.arraySize++;
						clips.GetArrayElementAtIndex(index).objectReferenceValue = clip;
					}
				}
			}

			current.Use();
		}

		private static void EnsureStyles()
		{
			if (s_lengthStyle != null) {
				return;
			}

			s_lengthStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
			s_playContent = new GUIContent(EditorGUIUtility.IconContent("PlayButton")) { tooltip = "Play this clip" };
		}
	}
}
