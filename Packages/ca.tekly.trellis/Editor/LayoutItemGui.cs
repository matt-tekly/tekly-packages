using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Trellis
{
	/// <summary>
	/// Draws the LayoutItem settings: a Width/Height table of size overrides that shows the value
	/// currently in effect for anything not overridden, then margin and advanced settings.
	/// </summary>
	public static class LayoutItemGui
	{
		private const float TOGGLE_WIDTH = 16f;
		private const float COLUMN_GAP = 6f;
		private const float SECTION_GAP = 2f;

		private static readonly GUIContent[] s_rowLabels = {
			new GUIContent("Min", "Never smaller than this"),
			new GUIContent("Preferred", "The size it asks for when there's room"),
			new GUIContent("Flexible", "Weight for sharing spare room. 0 means it doesn't grow"),
			new GUIContent("Max", "Never bigger than this. Min wins if they conflict. Only Trellis layouts use it")
		};

		// [row, axis]
		private static readonly string[,] s_sizeProperties = {
			{ "m_minWidth", "m_minHeight" },
			{ "m_preferredWidth", "m_preferredHeight" },
			{ "m_flexibleWidth", "m_flexibleHeight" },
			{ "m_maxWidth", "m_maxHeight" }
		};

		private static readonly GUIContent[] s_columnLabels = {
			new GUIContent("Width"),
			new GUIContent("Height")
		};

		private static readonly GUIContent s_advancedLabel = new GUIContent("Advanced");

		private static readonly GUIContent s_ignoreItemLabel = new GUIContent("Ignore Layout",
			"Parent layouts skip this object. Under a Trellis layout, disabling or removing this component " +
			"does the same; this is mainly for Unity layout groups");

		private static readonly GUIContent s_ignoreLayoutLabel = new GUIContent("Ignore Layout",
			"The parent layout doesn't place or size this object, but it still arranges its own children. " +
			"Use it for overlays and badges that sit on top of the parent's flow");

		private static readonly GUIContent s_overrideTooltip = new GUIContent("", "Override this size");

		private static readonly GUIContent s_sizeLabel = new GUIContent("Size",
			"Tick a value to override it. Unticked values show what's in effect now");

		private static GUIStyle s_headerStyle;
		private static GUIStyle s_currentStyle;
		private static bool s_advancedExpanded;

		/// <summary>
		/// Draws the item settings. Nested draws them flat, for use inside another section.
		/// </summary>
		public static void Draw(SerializedObject serializedObject, bool nested = false)
		{
			EnsureStyles();

			var item = serializedObject.targetObjects.Length == 1 ? serializedObject.targetObject as LayoutItem : null;
			var ignore = serializedObject.FindProperty("m_ignoreLayout");

			// Layouts need Ignore Layout to leave their parent's flow and keep arranging children.
			// Plain items can just disable the component, so it lives in Advanced unless it's in use.
			var isLayout = serializedObject.targetObject is LayoutContainer;
			var ignoreAtTop = isLayout || ignore.boolValue || ignore.hasMultipleDifferentValues;
			var ignored = ignore.boolValue && !ignore.hasMultipleDifferentValues;

			using (BeginGroup(nested)) {
				if (ignoreAtTop) {
					EditorGUILayout.PropertyField(ignore, isLayout ? s_ignoreLayoutLabel : s_ignoreItemLabel);
					ignored = ignore.boolValue && !ignore.hasMultipleDifferentValues;
				}

				using (new EditorGUI.DisabledScope(ignored)) {
					DrawSizeTable(serializedObject, item);
					EditorGUILayout.Space(SECTION_GAP);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_margin"));
				}
			}

			using (new EditorGUI.DisabledScope(ignored)) {
				if (nested) {
					s_advancedExpanded = TrellisSection.Foldout(s_advancedExpanded, s_advancedLabel);
					DrawAdvanced(serializedObject, ignore, ignoreAtTop, s_advancedExpanded);
				} else {
					using (var advanced = TrellisSection.BeginFoldout(s_advancedLabel, s_advancedExpanded)) {
						s_advancedExpanded = advanced.Expanded;
						DrawAdvanced(serializedObject, ignore, ignoreAtTop, advanced.Expanded);
					}
				}
			}

			if (item != null && !ignored) {
				DrawParentNote(item);
			}
		}

		private static void DrawAdvanced(SerializedObject serializedObject, SerializedProperty ignore, bool ignoreAtTop,
			bool expanded)
		{
			if (!expanded) {
				return;
			}

			EditorGUI.indentLevel++;

			if (!ignoreAtTop) {
				EditorGUILayout.PropertyField(ignore, s_ignoreItemLabel);
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_layoutPriority"));
			EditorGUI.indentLevel--;
		}

		/// <summary>
		/// A box on its own inspector; nothing when nested inside another section.
		/// </summary>
		private static IDisposable BeginGroup(bool nested)
		{
			return nested ? (IDisposable) new EditorGUILayout.VerticalScope() : TrellisSection.Begin();
		}

		private static void DrawSizeTable(SerializedObject serializedObject, LayoutItem item)
		{
			var header = EditorGUILayout.GetControlRect();
			var headerCells = EditorGUI.PrefixLabel(header, GUIUtility.GetControlID(FocusType.Passive), s_sizeLabel,
				EditorStyles.boldLabel);
			SplitColumns(headerCells, out var headerWidth, out var headerHeight);

			GUI.Label(headerWidth, s_columnLabels[0], s_headerStyle);
			GUI.Label(headerHeight, s_columnLabels[1], s_headerStyle);

			var measureWidth = item != null ? item.Measure(0) : default;
			var measureHeight = item != null ? item.Measure(1) : default;

			for (var row = 0; row < s_rowLabels.Length; row++) {
				var rect = EditorGUILayout.GetControlRect();
				var cells = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), s_rowLabels[row]);
				SplitColumns(cells, out var widthCell, out var heightCell);

				var indent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;

				DrawSizeCell(widthCell, serializedObject.FindProperty(s_sizeProperties[row, 0]), item != null,
					CurrentValue(measureWidth, row));
				DrawSizeCell(heightCell, serializedObject.FindProperty(s_sizeProperties[row, 1]), item != null,
					CurrentValue(measureHeight, row));

				EditorGUI.indentLevel = indent;
			}
		}

		/// <summary>
		/// An override toggle and its value. Without an override, shows the value in effect (greyed).
		/// Turning an override on starts it from that value.
		/// </summary>
		private static void DrawSizeCell(Rect rect, SerializedProperty property, bool hasCurrent, float current)
		{
			var isSet = property.FindPropertyRelative("IsSet");
			var value = property.FindPropertyRelative("Value");

			EditorGUI.BeginProperty(rect, GUIContent.none, property);

			var toggleRect = new Rect(rect.x, rect.y, TOGGLE_WIDTH, rect.height);
			var fieldRect = new Rect(toggleRect.xMax, rect.y, rect.width - TOGGLE_WIDTH, rect.height);

			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = isSet.hasMultipleDifferentValues;

			var set = EditorGUI.Toggle(toggleRect, isSet.boolValue);

			EditorGUI.showMixedValue = false;
			GUI.Label(toggleRect, s_overrideTooltip, GUIStyle.none);

			if (EditorGUI.EndChangeCheck()) {
				isSet.boolValue = set;
				value.floatValue = set && hasCurrent && !float.IsInfinity(current) ? current : 0f;
			}

			if (isSet.boolValue || isSet.hasMultipleDifferentValues) {
				EditorGUI.showMixedValue = value.hasMultipleDifferentValues;
				EditorGUI.BeginChangeCheck();

				var newValue = EditorGUI.FloatField(fieldRect, value.floatValue);

				if (EditorGUI.EndChangeCheck()) {
					value.floatValue = Mathf.Max(0f, newValue);
				}

				EditorGUI.showMixedValue = false;
			} else {
				GUI.Label(fieldRect, hasCurrent ? Format(current) : "—", s_currentStyle);
			}

			EditorGUI.EndProperty();
		}

		private static void DrawParentNote(LayoutItem item)
		{
			var parent = item.transform.parent;

			if (parent == null) {
				return;
			}

			var group = FindSizingGroup(parent);

			if (group is LayoutContainer) {
				return;
			}

			if (group != null) {
				EditorGUILayout.HelpBox("The parent is a Unity layout group: it uses Min, Preferred and Flexible " +
					"but ignores Max and Margin.", MessageType.Info);
				return;
			}

			// A layout uses its own Item sizes for Fit, so the settings still matter without a parent layout
			if (!(item is LayoutContainer)) {
				EditorGUILayout.HelpBox("The parent isn't a layout, so these settings have no effect.", MessageType.Info);
			}
		}

		/// <summary>
		/// The enabled layout group on the parent that sizes its children, skipping relays, proxies and ScrollRects.
		/// </summary>
		private static Component FindSizingGroup(Transform parent)
		{
			foreach (var component in parent.GetComponents(typeof(ILayoutGroup))) {
				if (component is Behaviour behaviour && behaviour.isActiveAndEnabled
					&& LayoutContainer.SizesChildren(component)) {
					return component;
				}
			}

			return null;
		}

		private static float CurrentValue(LayoutMeasure measure, int row)
		{
			switch (row) {
				case 0:
					return measure.Min;
				case 1:
					return measure.Preferred;
				case 2:
					return measure.Flexible;
				default:
					return measure.Max;
			}
		}

		private static string Format(float value)
		{
			return float.IsPositiveInfinity(value) ? "none" : value.ToString("0.##", CultureInfo.InvariantCulture);
		}

		private static void SplitColumns(Rect rect, out Rect left, out Rect right)
		{
			var width = (rect.width - COLUMN_GAP) * 0.5f;
			left = new Rect(rect.x, rect.y, width, rect.height);
			right = new Rect(left.xMax + COLUMN_GAP, rect.y, width, rect.height);
		}

		private static void EnsureStyles()
		{
			if (s_headerStyle != null) {
				return;
			}

			s_headerStyle = new GUIStyle(EditorStyles.miniBoldLabel) {
				alignment = TextAnchor.MiddleCenter
			};

			s_currentStyle = new GUIStyle(EditorStyles.label) {
				fontStyle = FontStyle.Italic
			};

			s_currentStyle.normal.textColor = EditorStyles.centeredGreyMiniLabel.normal.textColor;
		}
	}
}
