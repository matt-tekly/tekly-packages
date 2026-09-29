using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Trellis
{
	[CustomEditor(typeof(LayoutProxy))]
	[CanEditMultipleObjects]
	public class LayoutProxyEditor : Editor
	{
		private static readonly GUIContent s_sourceLabel = new GUIContent("Source",
			"Where this object's size comes from");
		private static readonly GUIContent s_fitLabel = new GUIContent("Fit",
			"Resize this object to the source, when no parent layout sizes it");
		private static readonly GUIContent s_itemLabel = new GUIContent("Item",
			"How this object is sized and placed by its parent. Max Height here caps a growing scroll view");

		private static readonly List<GameObject> s_missingRelays = new List<GameObject>();

		private static bool s_itemExpanded = true;

		private SerializedProperty m_source;

		private void OnEnable()
		{
			m_source = serializedObject.FindProperty("m_source");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var proxy = targets.Length == 1 ? (LayoutProxy) target : null;

			using (TrellisSection.Begin(s_sourceLabel)) {
				EditorGUILayout.PropertyField(m_source);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_useSourceWidth"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_useSourceHeight"));

				if (proxy != null) {
					DrawSourceNotes(proxy);
				}
			}

			using (TrellisSection.Begin(s_fitLabel)) {
				TrellisEditorGui.DrawFit(serializedObject);
			}

			using (var item = TrellisSection.BeginFoldout(s_itemLabel, s_itemExpanded)) {
				s_itemExpanded = item.Expanded;

				if (item.Expanded) {
					LayoutItemGui.Draw(serializedObject, true);
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		private static void DrawSourceNotes(LayoutProxy proxy)
		{
			var source = proxy.ResolvedSource;

			if (source == null) {
				var message = proxy.Source == null
					? "No source: assign one, or add a ScrollRect with Content set."
					: "The source must be a descendant of this object.";

				EditorGUILayout.HelpBox(message, MessageType.Warning);
				return;
			}

			if (proxy.Source == null) {
				EditorGUILayout.LabelField(" ", $"Using ScrollRect Content: {source.name}", EditorStyles.miniLabel);
			}

			DrawMissingRelays(proxy, source);
			DrawSourceFitNote(proxy, source);
		}

		/// <summary>
		/// Every object between the source and the proxy needs a layout group for rebuilds to reach the proxy.
		/// </summary>
		private static void DrawMissingRelays(LayoutProxy proxy, RectTransform source)
		{
			s_missingRelays.Clear();

			for (var current = source.parent; current != null && current != proxy.transform; current = current.parent) {
				if (!HasEnabledGroup(current)) {
					s_missingRelays.Add(current.gameObject);
				}
			}

			if (s_missingRelays.Count == 0) {
				return;
			}

			var names = string.Join(", ", s_missingRelays.ConvertAll(go => go.name));

			EditorGUILayout.HelpBox($"Needs a LayoutRelay so changes in the source reach this object: {names}",
				MessageType.Warning);

			if (GUILayout.Button("Add LayoutRelay")) {
				foreach (var go in s_missingRelays) {
					Undo.AddComponent<LayoutRelay>(go);
				}
			}
		}

		/// <summary>
		/// A scroll view's content has to grow with its children, or there is nothing to scroll.
		/// </summary>
		private static void DrawSourceFitNote(LayoutProxy proxy, RectTransform source)
		{
			if (source.GetComponent<ContentSizeFitter>() != null) {
				return;
			}

			var container = source.GetComponent<LayoutContainer>();

			if (container == null) {
				return;
			}

			var widthMissing = proxy.UseSourceWidth && container.FitWidth == FitMode.None;
			var heightMissing = proxy.UseSourceHeight && container.FitHeight == FitMode.None;

			if (widthMissing || heightMissing) {
				var axis = heightMissing ? "Height" : "Width";

				EditorGUILayout.HelpBox($"Set {source.name}'s Fit {axis} to Preferred so it grows with its content. " +
					"Otherwise a ScrollRect has nothing to scroll.", MessageType.Info);
			}
		}

		private static bool HasEnabledGroup(Transform transform)
		{
			foreach (var component in transform.GetComponents(typeof(ILayoutGroup))) {
				if (component is Behaviour behaviour && behaviour.isActiveAndEnabled) {
					return true;
				}
			}

			return false;
		}
	}
}
