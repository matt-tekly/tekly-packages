using System;
using System.Globalization;
using Tekly.DevBoard.Components.Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.DevBoard.Components.Inputs
{
	/// <summary>
	/// An input for a number. With whole numbers on it takes integers only, which is how int inputs are made.
	/// Values are doubles, which hold every int exactly; the IntInput and FloatInput factories convert.
	///
	/// Dragging sideways on the label changes the value, like the labels in Unity's inspector. By default whole
	/// numbers change by 1 every 10 pixels, and others by 0.01 a pixel; WithDragStep sets the change per pixel.
	/// </summary>
	public class NumberInputWidget : InputWidget<double>
	{
		private const double WHOLE_DRAG_STEP = 0.1;
		private const double DECIMAL_DRAG_STEP = 0.01;

		[Tooltip("Integers only. Code sets this for IntInput and FloatInput")]
		[SerializeField] private bool m_wholeNumbers;

		private string m_format;
		private double? m_dragStep;
		private DragScrubber m_scrubber;

		// Whole numbers move by fractions of a step per pixel, so the drag adds up here between whole changes
		private double m_scrubValue;

		public bool WholeNumbers => m_wholeNumbers;

		/// <summary>
		/// Switches between integers only and any number.
		/// </summary>
		public NumberInputWidget WithWholeNumbers(bool wholeNumbers)
		{
			m_wholeNumbers = wholeNumbers;
			ApplyContentType();
			Invalidate();
			return this;
		}

		/// <summary>
		/// How much the value changes per pixel dragged on the label. Null for the default.
		/// </summary>
		public NumberInputWidget WithDragStep(double? step)
		{
			m_dragStep = step;
			return this;
		}

		/// <summary>
		/// Turns changing the value by dragging on the label on or off. On by default.
		/// </summary>
		public NumberInputWidget WithDragScrub(bool enabled)
		{
			// Disabled, it gets no events, so drags on the label behave as they did without it
			if (m_scrubber != null) {
				m_scrubber.enabled = enabled;
			}

			return this;
		}

		/// <summary>
		/// The text shown while the field is empty.
		/// </summary>
		public NumberInputWidget WithPlaceholder(string placeholder)
		{
			SetPlaceholder(placeholder);
			return this;
		}

		/// <summary>
		/// When typed numbers reach the setter. Delayed by default.
		/// </summary>
		public NumberInputWidget WithInputMode(InputMode inputMode)
		{
			SetInputMode(inputMode);
			return this;
		}

		/// <summary>
		/// Sets the numeric format string used to display the value, e.g. "0.00".
		/// </summary>
		public NumberInputWidget WithFormat(string format)
		{
			m_format = string.IsNullOrEmpty(format) ? null : format;
			Invalidate();
			return this;
		}

		protected override void Awake()
		{
			base.Awake();
			ApplyContentType();
			AddScrubber();
		}

		private void AddScrubber()
		{
			var label = LabelComponent;

			if (label == null) {
				return;
			}

			// The label has to take presses for drags to start on it
			label.TextComponent.raycastTarget = true;

			m_scrubber = label.gameObject.AddComponent<DragScrubber>();
			m_scrubber.Began = OnScrubBegan;
			m_scrubber.Scrubbed = OnScrubbed;
		}

		private void OnScrubBegan()
		{
			// A press on the label focuses the field; a scrub isn't an edit, so stop editing
			m_input.DeactivateInputField();

			if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == m_input.gameObject) {
				EventSystem.current.SetSelectedGameObject(null);
			}

			m_scrubValue = TryGetValue(out var value) ? value : 0;
		}

		private void OnScrubbed(float pixels)
		{
			var step = m_dragStep ?? (m_wholeNumbers ? WHOLE_DRAG_STEP : DECIMAL_DRAG_STEP);
			m_scrubValue += pixels * step;

			var value = m_wholeNumbers ? Math.Round(m_scrubValue) : m_scrubValue;

			if (!TryGetValue(out var current) || value != current) {
				SetValue(value);
			}
		}

		protected override bool TryParse(string text, out double value)
		{
			return NumberText.TryParse(text, m_wholeNumbers, out value);
		}

		protected override string Format(double value)
		{
			return NumberText.FormatNumber(value, m_wholeNumbers, m_format);
		}

		private void ApplyContentType()
		{
			m_input.contentType = m_wholeNumbers
				? TMP_InputField.ContentType.IntegerNumber
				: TMP_InputField.ContentType.DecimalNumber;
		}
	}
}

namespace Tekly.DevBoard.Components
{
	public static class NumberInputWidgetFactory
	{
		/// <summary>
		/// A number input over doubles. Use WithWholeNumbers for integers only.
		/// </summary>
		public static NumberInputWidget NumberInput(this ContainerWidget parent, string label, Func<double> getValue,
			Action<double> setValue, WidgetPrefab<NumberInputWidget> prefab = default)
		{
			return parent.NumberInput(label, new Binding<double>(getValue, setValue), prefab);
		}

		public static NumberInputWidget NumberInput(this ContainerWidget parent, string label, Binding<double> binding,
			WidgetPrefab<NumberInputWidget> prefab = default)
		{
			var instance = parent.Create(prefab.Or(WidgetPrefabs.NumberInput));
			instance.Initialize(label, binding);

			return instance;
		}

		public static NumberInputWidget IntInput(this ContainerWidget parent, string label, Func<int> getValue, Action<int> setValue,
			WidgetPrefab<NumberInputWidget> prefab = default)
		{
			var binding = new Binding<int>(getValue, setValue).Convert(value => (double) value, ToInt);
			return parent.NumberInput(label, binding, prefab).WithWholeNumbers(true);
		}

		public static NumberInputWidget FloatInput(this ContainerWidget parent, string label, Func<float> getValue, Action<float> setValue,
			WidgetPrefab<NumberInputWidget> prefab = default)
		{
			var binding = new Binding<float>(getValue, setValue).Convert(value => (double) value, value => (float) value);
			return parent.NumberInput(label, binding, prefab).WithWholeNumbers(false);
		}

		/// <summary>
		/// A typed number as an int, clamped to the int range.
		/// </summary>
		private static int ToInt(double value)
		{
			return (int) Math.Round(Math.Clamp(value, int.MinValue, int.MaxValue));
		}
	}
}
