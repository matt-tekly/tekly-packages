using Tekly.Leaf.Elements.Radios;
using UnityEditor;
using UnityEngine;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// The On checkbox shown on radio options. Options don't store whether they're on: in edit mode this sets the
	/// group's Initial Option, in play mode it turns the option on through the group.
	/// </summary>
	internal static class LeafRadioOptionInspector
	{
		private static readonly GUIContent s_onLabel = new("On",
			"Whether this option is on. In edit mode this sets the group's Initial Option");

		private static readonly GUIContent s_requiredLabel = new("On",
			"The group needs an option on. Turn another one on instead, or allow none on the group");

		public static void DrawIsOn(Editor editor)
		{
			if (editor.targets.Length != 1 || editor.target is not Component component ||
			    component is not ILeafRadioOption option) {
				return;
			}

			var group = component.GetComponentInParent<LeafRadioGroup>(true);
			if (group == null) {
				// Without a group the option keeps its own on/off, which isn't serialized
				return;
			}

			var isOn = ReferenceEquals(group.Current, option);
			var canTurnOff = group.AllowNone;

			using (new EditorGUI.DisabledScope(isOn && !canTurnOff)) {
				var label = isOn && !canTurnOff ? s_requiredLabel : s_onLabel;
				var newIsOn = EditorGUILayout.Toggle(label, isOn);

				if (newIsOn != isOn) {
					SetIsOn(group, component, option, newIsOn);
				}
			}

			EditorGUILayout.Space();
		}

		private static void SetIsOn(LeafRadioGroup group, Component component, ILeafRadioOption option, bool isOn)
		{
			if (Application.isPlaying) {
				group.Select(isOn ? option : null);
				return;
			}

			// The group's OnValidate shows the change on the options
			var serializedGroup = new SerializedObject(group);
			serializedGroup.FindProperty("m_initialOption").objectReferenceValue = isOn ? component : null;
			serializedGroup.ApplyModifiedProperties();
		}
	}
}
