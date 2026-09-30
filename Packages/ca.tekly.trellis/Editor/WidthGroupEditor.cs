using UnityEditor;
using UnityEngine;

namespace Tekly.Trellis
{
	[CustomEditor(typeof(WidthGroup))]
	[CanEditMultipleObjects]
	public class WidthGroupEditor : Editor
	{
		private static readonly GUIContent s_groupsLabel = new GUIContent("Groups",
			"Width Group names used by LayoutItems below this object");

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			using (TrellisSection.Begin()) {
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maxWidth"));
			}

			serializedObject.ApplyModifiedProperties();

			if (targets.Length == 1) {
				DrawGroups((WidthGroup) target);
			}
		}

		private static void DrawGroups(WidthGroup group)
		{
			if (group.Members.Count == 0) {
				EditorGUILayout.HelpBox("Give LayoutItems below this object the same Width Group name " +
					"(for example \"label\") to line up their widths.", MessageType.Info);
				return;
			}

			using (TrellisSection.Begin(s_groupsLabel)) {
				foreach (var pair in group.Members) {
					var counted = 0;
					var width = 0f;

					foreach (var member in pair.Value) {
						if (member == null || !member.isActiveAndEnabled || member.IgnoreLayout) {
							continue;
						}

						counted++;
						width = Mathf.Max(width, member.Measure(0).Preferred);
					}

					var summary = $"{counted} item{(counted == 1 ? "" : "s")}, {width:0.#} wide";
					EditorGUILayout.LabelField(pair.Key, summary);
				}
			}
		}
	}
}
