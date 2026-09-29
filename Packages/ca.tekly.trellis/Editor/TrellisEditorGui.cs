using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tekly.Trellis
{
	/// <summary>
	/// Inspector pieces shared by Trellis layouts and LayoutItem.
	/// </summary>
	public static class TrellisEditorGui
	{
		private const int MAX_LISTED_NAMES = 4;

		private static readonly List<GameObject> s_missing = new List<GameObject>();

		/// <summary>
		/// Draws the LayoutItem fields: how this object is sized by its parent.
		/// </summary>
		public static void DrawItemProperties(SerializedObject serializedObject)
		{
			LayoutItemGui.Draw(serializedObject);
		}

		/// <summary>
		/// Draws Fit Width and Fit Height, noting when a parent layout sizes the object instead.
		/// </summary>
		public static void DrawFit(SerializedObject serializedObject)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_fitWidth"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_fitHeight"));

			if (serializedObject.targetObjects.Length != 1) {
				return;
			}

			var container = (LayoutContainer) serializedObject.targetObject;

			if (container.IsLaidOutByParent && (container.FitWidth != FitMode.None || container.FitHeight != FitMode.None)) {
				EditorGUILayout.HelpBox("A parent layout sizes this object, so Fit does nothing. " +
					"Set its size with the Item settings instead.", MessageType.Info);
			}
		}

		/// <summary>
		/// Lists children that will be skipped because they have no LayoutItem, with a button to add one.
		/// </summary>
		public static void DrawMissingItems(Transform parent)
		{
			s_missing.Clear();

			foreach (Transform child in parent) {
				if (child is RectTransform && child.GetComponent<LayoutItem>() == null) {
					s_missing.Add(child.gameObject);
				}
			}

			if (s_missing.Count == 0) {
				return;
			}

			var names = string.Join(", ", s_missing.Take(MAX_LISTED_NAMES).Select(go => go.name));

			if (s_missing.Count > MAX_LISTED_NAMES) {
				names += $" and {s_missing.Count - MAX_LISTED_NAMES} more";
			}

			EditorGUILayout.HelpBox($"Not laid out (no LayoutItem): {names}", MessageType.Info);

			if (GUILayout.Button("Add LayoutItem to these children")) {
				foreach (var child in s_missing) {
					Undo.AddComponent<LayoutItem>(child);
				}
			}
		}
	}
}
