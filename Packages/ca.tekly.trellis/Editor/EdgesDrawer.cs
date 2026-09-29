using UnityEditor;
using UnityEngine;

namespace Tekly.Trellis
{
	/// <summary>
	/// Draws Edges on one line: a link toggle, then Left, Right, Top and Bottom.
	/// With the link on, editing any side sets all four. Drops to a second line in narrow inspectors.
	/// Drag the L/R/T/B labels to scrub values.
	/// </summary>
	[CustomPropertyDrawer(typeof(Edges))]
	public class EdgesDrawer : PropertyDrawer
	{
		private const float LINK_WIDTH = 18f;
		private const float SIDE_LABEL_WIDTH = 13f;
		private const float GAP = 4f;
		private const string LINK_KEY_PREFIX = "Tekly.Trellis.EdgesLinked.";

		private static readonly string[] s_sides = {
			"Left",
			"Right",
			"Top",
			"Bottom"
		};

		private static readonly GUIContent[] s_sideLabels = {
			new GUIContent("L", "Left"),
			new GUIContent("R", "Right"),
			new GUIContent("T", "Top"),
			new GUIContent("B", "Bottom")
		};

		private static GUIContent s_linkedContent;
		private static GUIContent s_unlinkedContent;
		private static GUIStyle s_linkStyle;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var line = EditorGUIUtility.singleLineHeight;
			return EditorGUIUtility.wideMode ? line : line * 2f + EditorGUIUtility.standardVerticalSpacing;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EnsureStyles();

			label = EditorGUI.BeginProperty(position, label, property);

			var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			var content = EditorGUI.PrefixLabel(line, GUIUtility.GetControlID(FocusType.Passive), label);

			if (!EditorGUIUtility.wideMode) {
				var second = new Rect(position.x, line.yMax + EditorGUIUtility.standardVerticalSpacing,
					position.width, EditorGUIUtility.singleLineHeight);
				content = EditorGUI.IndentedRect(second);
			}

			var indent = EditorGUI.indentLevel;
			var labelWidth = EditorGUIUtility.labelWidth;
			EditorGUI.indentLevel = 0;

			var linked = DrawLink(new Rect(content.x, content.y, LINK_WIDTH, content.height), property);

			EditorGUIUtility.labelWidth = SIDE_LABEL_WIDTH;

			var fieldsX = content.x + LINK_WIDTH + GAP;
			var cellWidth = (content.xMax - fieldsX) / s_sides.Length;

			for (var i = 0; i < s_sides.Length; i++) {
				var cell = new Rect(fieldsX + cellWidth * i, content.y, cellWidth - GAP, content.height);
				DrawSide(cell, property, i, linked);
			}

			EditorGUIUtility.labelWidth = labelWidth;
			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty();
		}

		private static bool DrawLink(Rect rect, SerializedProperty property)
		{
			var key = LinkKey(property);
			var linked = SessionState.GetBool(key, false);
			var toggled = GUI.Toggle(rect, linked, linked ? s_linkedContent : s_unlinkedContent, s_linkStyle);

			if (toggled == linked) {
				return linked;
			}

			SessionState.SetBool(key, toggled);

			// Linking makes the sides uniform, starting from Left
			if (toggled) {
				var value = property.FindPropertyRelative(s_sides[0]).floatValue;
				SetAll(property, value);
			}

			return toggled;
		}

		private static void DrawSide(Rect rect, SerializedProperty property, int index, bool linked)
		{
			var side = property.FindPropertyRelative(s_sides[index]);

			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = side.hasMultipleDifferentValues;

			var value = EditorGUI.FloatField(rect, s_sideLabels[index], side.floatValue);

			EditorGUI.showMixedValue = false;

			if (!EditorGUI.EndChangeCheck()) {
				return;
			}

			if (linked) {
				SetAll(property, value);
			} else {
				side.floatValue = value;
			}
		}

		private static void SetAll(SerializedProperty property, float value)
		{
			foreach (var name in s_sides) {
				property.FindPropertyRelative(name).floatValue = value;
			}
		}

		private static string LinkKey(SerializedProperty property)
		{
			var target = property.serializedObject.targetObject;
			return LINK_KEY_PREFIX + target.GetInstanceID() + "." + property.propertyPath;
		}

		private static void EnsureStyles()
		{
			if (s_linkStyle != null) {
				return;
			}

			s_linkStyle = new GUIStyle(EditorStyles.label) {
				alignment = TextAnchor.MiddleCenter,
				padding = new RectOffset(0, 0, 0, 0)
			};

			s_linkedContent = IconOrText("Linked", "=", "Linked: editing one side sets all four");
			s_unlinkedContent = IconOrText("Unlinked", "≠", "Unlinked: sides are edited separately");
		}

		private static GUIContent IconOrText(string icon, string fallback, string tooltip)
		{
			var image = EditorGUIUtility.IconContent(icon).image;
			return image != null ? new GUIContent(image, tooltip) : new GUIContent(fallback, tooltip);
		}
	}
}
