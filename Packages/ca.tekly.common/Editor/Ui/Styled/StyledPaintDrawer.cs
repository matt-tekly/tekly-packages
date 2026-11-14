using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.Common.Ui.Styled
{
	/// <summary>
	/// Draws a StyledPaint: its color, then the gradient type and only the settings that gradient uses.
	/// </summary>
	[CustomPropertyDrawer(typeof(StyledPaint))]
	public class StyledPaintDrawer : PropertyDrawer
	{
		private const float DETAIL_INDENT = 15f;
		private const float RANGE_NUMBER_WIDTH = 44f;

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var gradient = property.FindPropertyRelative(nameof(StyledPaint.Gradient));

			var root = new VisualElement();
			root.Add(new PropertyField(property.FindPropertyRelative(nameof(StyledPaint.Color)), property.displayName));

			var details = new VisualElement();
			details.style.marginLeft = DETAIL_INDENT;
			root.Add(details);

			var color2 = new PropertyField(property.FindPropertyRelative(nameof(StyledPaint.Color2)), "Color 2");
			var angle = new PropertyField(property.FindPropertyRelative(nameof(StyledPaint.Angle)));
			var range = CreateRange(property.FindPropertyRelative(nameof(StyledPaint.RangeStart)), property.FindPropertyRelative(nameof(StyledPaint.RangeEndInset)));
			var bias = new PropertyField(property.FindPropertyRelative(nameof(StyledPaint.Bias)));

			details.Add(new PropertyField(gradient));
			details.Add(color2);
			details.Add(angle);
			details.Add(range);
			details.Add(bias);

			void Refresh()
			{
				var mixed = gradient.hasMultipleDifferentValues;
				var type = (StyledGradient)gradient.intValue;

				SetVisible(color2, mixed || type != StyledGradient.None);
				SetVisible(angle, mixed || type == StyledGradient.Linear);
				SetVisible(range, mixed || type != StyledGradient.None);
				SetVisible(bias, mixed || type != StyledGradient.None);
			}

			root.TrackPropertyValue(gradient, _ => Refresh());
			Refresh();

			return root;
		}

		/// <summary>
		/// Min-max slider with a number box at each end for where the gradient runs. The end is stored as an
		/// inset, so the slider is synced by hand and not bound.
		/// </summary>
		private static VisualElement CreateRange(SerializedProperty rangeStart, SerializedProperty rangeEndInset)
		{
			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;

			var slider = new MinMaxSlider("Range", 0f, 1f, 0f, 1f) {
				tooltip = "Where the gradient runs. Before it is solid Color, after it solid Color 2."
			};
			slider.AddToClassList(BaseField<Vector2>.alignedFieldUssClassName);
			slider.style.flexGrow = 1f;

			var startBox = CreateNumberBox();
			var endBox = CreateNumberBox();

			row.Add(slider);
			row.Add(startBox);
			row.Add(endBox);

			void Show()
			{
				var start = rangeStart.floatValue;
				var end = 1f - rangeEndInset.floatValue;

				slider.SetValueWithoutNotify(new Vector2(start, end));
				startBox.SetValueWithoutNotify(start);
				endBox.SetValueWithoutNotify(end);
			}

			void Store(float start, float end)
			{
				start = Mathf.Clamp01(start);
				end = Mathf.Clamp(end, start, 1f);

				rangeStart.floatValue = start;
				rangeEndInset.floatValue = 1f - end;
				rangeStart.serializedObject.ApplyModifiedProperties();
				Show();
			}

			slider.RegisterValueChangedCallback(evt => Store(evt.newValue.x, evt.newValue.y));
			startBox.RegisterValueChangedCallback(evt => Store(evt.newValue, 1f - rangeEndInset.floatValue));
			endBox.RegisterValueChangedCallback(evt => Store(rangeStart.floatValue, evt.newValue));

			row.TrackPropertyValue(rangeStart, _ => Show());
			row.TrackPropertyValue(rangeEndInset, _ => Show());
			Show();

			return row;
		}

		private static FloatField CreateNumberBox()
		{
			var box = new FloatField { isDelayed = true };
			box.style.width = RANGE_NUMBER_WIDTH;
			return box;
		}

		private static void SetVisible(VisualElement element, bool visible)
		{
			element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}
}
