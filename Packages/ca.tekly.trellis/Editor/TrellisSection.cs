using System;
using UnityEditor;
using UnityEngine;

namespace Tekly.Trellis
{
	/// <summary>
	/// Boxed inspector sections. Use with <c>using</c>:
	/// <code>
	/// using (TrellisSection.Begin(title)) { ... }
	/// using (var section = TrellisSection.BeginFoldout(title, s_expanded)) {
	///     s_expanded = section.Expanded;
	///     if (section.Expanded) { ... }
	/// }
	/// </code>
	/// </summary>
	public readonly struct TrellisSection : IDisposable
	{
		private const float SPACE_AFTER = 2f;

		// Inspectors draw foldout arrows in the margin left of the rect; inside a box that clips the border
		private const float FOLDOUT_ARROW_WIDTH = 14f;

		private static GUIStyle s_boxStyle;
		private static GUIStyle s_foldoutStyle;

		public readonly bool Expanded;

		private TrellisSection(bool expanded)
		{
			Expanded = expanded;
		}

		/// <summary>
		/// A box with an optional bold title.
		/// </summary>
		public static TrellisSection Begin(GUIContent title = null)
		{
			EnsureStyles();
			EditorGUILayout.BeginVertical(s_boxStyle);

			if (title != null) {
				EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
			}

			return new TrellisSection(true);
		}

		/// <summary>
		/// A box whose bold title folds its content away.
		/// </summary>
		public static TrellisSection BeginFoldout(GUIContent title, bool expanded)
		{
			EnsureStyles();
			EditorGUILayout.BeginVertical(s_boxStyle);

			return new TrellisSection(Foldout(expanded, title, s_foldoutStyle));
		}

		/// <summary>
		/// A box whose title is a bold toggle for a bool property. Expanded while it's on (or mixed).
		/// </summary>
		public static TrellisSection BeginToggle(SerializedProperty property, GUIContent title)
		{
			EnsureStyles();
			EditorGUILayout.BeginVertical(s_boxStyle);

			var rect = EditorGUILayout.GetControlRect();

			EditorGUI.BeginProperty(rect, title, property);
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

			var value = EditorGUI.ToggleLeft(rect, title, property.boolValue, EditorStyles.boldLabel);

			EditorGUI.showMixedValue = false;

			if (EditorGUI.EndChangeCheck()) {
				property.boolValue = value;
			}

			EditorGUI.EndProperty();

			return new TrellisSection(property.boolValue || property.hasMultipleDifferentValues);
		}

		/// <summary>
		/// A foldout that keeps its arrow inside the current box.
		/// </summary>
		public static bool Foldout(bool expanded, GUIContent title, GUIStyle style = null)
		{
			var rect = EditorGUILayout.GetControlRect();

			if (EditorGUIUtility.hierarchyMode) {
				rect.xMin += FOLDOUT_ARROW_WIDTH;
			}

			return EditorGUI.Foldout(rect, expanded, title, true, style ?? EditorStyles.foldout);
		}

		public void Dispose()
		{
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space(SPACE_AFTER);
		}

		private static void EnsureStyles()
		{
			if (s_boxStyle != null) {
				return;
			}

			s_boxStyle = new GUIStyle(EditorStyles.helpBox) {
				padding = new RectOffset(8, 6, 4, 6)
			};

			s_foldoutStyle = new GUIStyle(EditorStyles.foldout) {
				fontStyle = FontStyle.Bold
			};
		}
	}
}
