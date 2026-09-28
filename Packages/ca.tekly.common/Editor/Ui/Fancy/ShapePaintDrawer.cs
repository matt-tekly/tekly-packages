using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Ui.Fancy
{
	[CustomPropertyDrawer(typeof(ShapePaint))]
	public class ShapePaintDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return GetLineCount(property) * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var color = property.FindPropertyRelative(nameof(ShapePaint.Color));
			var gradient = property.FindPropertyRelative(nameof(ShapePaint.Gradient));
			var color2 = property.FindPropertyRelative(nameof(ShapePaint.Color2));
			var angle = property.FindPropertyRelative(nameof(ShapePaint.Angle));
			var rangeStart = property.FindPropertyRelative(nameof(ShapePaint.RangeStart));
			var rangeEndInset = property.FindPropertyRelative(nameof(ShapePaint.RangeEndInset));
			var bias = property.FindPropertyRelative(nameof(ShapePaint.Bias));

			using (new EditorGUI.PropertyScope(position, label, property)) {
				var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
				var step = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				EditorGUI.PropertyField(line, color, label);
				line.y += step;

				using (new EditorGUI.IndentLevelScope()) {
					EditorGUI.PropertyField(line, gradient);
					line.y += step;

					var gradientType = (GradientType)gradient.enumValueIndex;

					if (gradientType != GradientType.None) {
						EditorGUI.PropertyField(line, color2, new GUIContent("Color 2"));
						line.y += step;
					}

					if (UsesAngle(gradientType)) {
						EditorGUI.PropertyField(line, angle);
						line.y += step;
					}

					if (gradientType != GradientType.None) {
						RangeGUI(line, rangeStart, rangeEndInset);
						line.y += step;

						EditorGUI.Slider(line, bias, -1f, 1f, new GUIContent("Bias", bias.tooltip));
					}
				}
			}
		}

		private static int GetLineCount(SerializedProperty property)
		{
			var gradient = property.FindPropertyRelative(nameof(ShapePaint.Gradient));
			var gradientType = gradient.hasMultipleDifferentValues ? GradientType.Linear : (GradientType)gradient.enumValueIndex;

			var lines = 2;

			if (gradientType != GradientType.None) {
				lines++;
			}

			if (UsesAngle(gradientType)) {
				lines++;
			}

			if (gradientType != GradientType.None) {
				lines += 2;
			}

			return lines;
		}

		/// <summary>
		/// Min-max slider for where the gradient runs. Stored as start + end inset so zeroed data is a full gradient.
		/// </summary>
		private static void RangeGUI(Rect line, SerializedProperty rangeStart, SerializedProperty rangeEndInset)
		{
			var label = new GUIContent("Range", "Where the gradient runs. Before it is solid Color, after it solid Color 2.");
			var start = rangeStart.floatValue;
			var end = 1f - rangeEndInset.floatValue;

			var numberWidth = 40f;
			var sliderRect = new Rect(line.x, line.y, line.width - (numberWidth + 4f) * 2f, line.height);
			var startRect = new Rect(sliderRect.xMax + 4f, line.y, numberWidth, line.height);
			var endRect = new Rect(startRect.xMax + 4f, line.y, numberWidth, line.height);

			EditorGUI.showMixedValue = rangeStart.hasMultipleDifferentValues || rangeEndInset.hasMultipleDifferentValues;
			EditorGUI.BeginChangeCheck();

			EditorGUI.MinMaxSlider(sliderRect, label, ref start, ref end, 0f, 1f);

			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			start = EditorGUI.FloatField(startRect, start);
			end = EditorGUI.FloatField(endRect, end);
			EditorGUI.indentLevel = indent;

			if (EditorGUI.EndChangeCheck()) {
				start = Mathf.Clamp01(start);
				end = Mathf.Clamp(end, start, 1f);
				rangeStart.floatValue = start;
				rangeEndInset.floatValue = 1f - end;
			}

			EditorGUI.showMixedValue = false;
		}

		private static bool UsesAngle(GradientType gradientType)
		{
			return gradientType == GradientType.Linear || gradientType == GradientType.Angular;
		}
	}
}
