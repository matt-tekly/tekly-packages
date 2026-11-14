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

				// With several objects selected the numbers are the first one's. Mark the ends that differ.
				startBox.showMixedValue = rangeStart.hasMultipleDifferentValues;
				endBox.showMixedValue = rangeEndInset.hasMultipleDifferentValues;
				slider.showMixedValue = startBox.showMixedValue || endBox.showMixedValue;
			}

			// Each end is written on its own, so with several objects selected, moving one end leaves the
			// other end of every object as it was.
			void StoreStart(float start)
			{
				rangeStart.floatValue = Mathf.Clamp(start, 0f, 1f - rangeEndInset.floatValue);
				rangeStart.serializedObject.ApplyModifiedProperties();
				Show();
			}

			void StoreEnd(float end)
			{
				rangeEndInset.floatValue = 1f - Mathf.Clamp(end, rangeStart.floatValue, 1f);
				rangeEndInset.serializedObject.ApplyModifiedProperties();
				Show();
			}

			slider.RegisterValueChangedCallback(evt => {
				// Compared with what is stored, since a mixed slider has no previous value to trust.
				var startMoved = !Mathf.Approximately(evt.newValue.x, rangeStart.floatValue);
				var endMoved = !Mathf.Approximately(evt.newValue.y, 1f - rangeEndInset.floatValue);

				// Dragging the whole range moves both. The end goes first when it moves up, so neither is
				// clamped against the other's old place.
				if (endMoved && evt.newValue.y > 1f - rangeEndInset.floatValue) {
					StoreEnd(evt.newValue.y);
					endMoved = false;
				}

				if (startMoved) {
					StoreStart(evt.newValue.x);
				}

				if (endMoved) {
					StoreEnd(evt.newValue.y);
				}
			});
			startBox.RegisterValueChangedCallback(evt => StoreStart(evt.newValue));
			endBox.RegisterValueChangedCallback(evt => StoreEnd(evt.newValue));

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
