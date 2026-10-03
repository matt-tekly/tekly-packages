using System.Collections.Generic;
using Tekly.Leaf.Elements.Radios;
using UnityEditor;
using UnityEngine;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Draws Initial Option as a list of the group's options rather than an object field, since dropping a
	/// GameObject on a MonoBehaviour field picks its first MonoBehaviour. In play mode it also shows the current
	/// option, and changing it selects through the group.
	/// </summary>
	[CustomEditor(typeof(LeafRadioGroup))]
	[CanEditMultipleObjects]
	public class LeafRadioGroupEditor : Editor
	{
		private const string INITIAL_OPTION = "m_initialOption";

		private static readonly GUIContent s_initialLabel = new("Initial Option",
			"The option that's on when the group starts");

		private static readonly GUIContent s_currentLabel = new("Current", "The option that's on right now");

		private readonly List<MonoBehaviour> m_options = new();
		private readonly List<ILeafRadioOption> m_found = new();

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var property = serializedObject.GetIterator();
			for (var enterChildren = true; property.NextVisible(enterChildren); enterChildren = false) {
				if (property.propertyPath == "m_Script") {
					using (new EditorGUI.DisabledScope(true)) {
						EditorGUILayout.PropertyField(property);
					}
				} else if (property.propertyPath == INITIAL_OPTION && !serializedObject.isEditingMultipleObjects) {
					DrawInitialOption(property);
				} else {
					EditorGUILayout.PropertyField(property, true);
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawInitialOption(SerializedProperty property)
		{
			var group = (LeafRadioGroup) target;
			CollectOptions(group);

			var noneLabel = group.AllowNone ? "None" : "First Option";
			var labels = BuildLabels(noneLabel);

			using (new EditorGUI.DisabledScope(Application.isPlaying)) {
				var current = property.objectReferenceValue as MonoBehaviour;
				var index = current != null ? m_options.IndexOf(current) + 1 : 0;

				if (index == 0 && current != null) {
					EditorGUILayout.HelpBox($"[{current.name}] isn't an option of this group, so the group starts with: {noneLabel}", MessageType.Warning);
				}

				var newIndex = EditorGUILayout.Popup(s_initialLabel, index, labels);
				if (newIndex != index) {
					property.objectReferenceValue = newIndex > 0 ? m_options[newIndex - 1] : null;
				}
			}

			if (Application.isPlaying) {
				DrawCurrent(group);
			}
		}

		private void DrawCurrent(LeafRadioGroup group)
		{
			var labels = BuildLabels("None");
			var index = group.Current is MonoBehaviour current ? m_options.IndexOf(current) + 1 : 0;

			var newIndex = EditorGUILayout.Popup(s_currentLabel, index, labels);
			if (newIndex == index) {
				return;
			}

			if (newIndex == 0) {
				if (group.AllowNone) {
					group.Select(null);
				}
			} else {
				group.Select(m_options[newIndex - 1] as ILeafRadioOption);
			}
		}

		/// <summary>
		/// The group's options in hierarchy order, hidden ones included. Options of nested groups are left out.
		/// </summary>
		private void CollectOptions(LeafRadioGroup group)
		{
			m_options.Clear();
			group.GetComponentsInChildren(true, m_found);

			for (var i = 0; i < m_found.Count; i++) {
				if (m_found[i] is MonoBehaviour behaviour && behaviour.GetComponentInParent<LeafRadioGroup>(true) == group) {
					m_options.Add(behaviour);
				}
			}

			m_found.Clear();
		}

		private GUIContent[] BuildLabels(string noneLabel)
		{
			var labels = new GUIContent[m_options.Count + 1];
			labels[0] = new GUIContent(noneLabel);

			for (var i = 0; i < m_options.Count; i++) {
				// Numbered, so options with the same name can still be told apart
				labels[i + 1] = new GUIContent($"{i + 1}: {m_options[i].name}");
			}

			return labels;
		}
	}
}
