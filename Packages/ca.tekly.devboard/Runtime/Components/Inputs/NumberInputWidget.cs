using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.DevBoard.Components.Inputs
{
	/// <summary>
	/// An input for a number. With whole numbers on it takes integers only, which is how int inputs are made.
	/// Values are doubles, which hold every int exactly; ContainerWidget.IntInput and FloatInput convert.
	///
	/// Dragging sideways on the label changes the value, like the labels in Unity's inspector. By default whole
	/// numbers change by 1 every 10 pixels, and others by 0.01 a pixel; WithDragStep sets the change per pixel.
	/// </summary>
	public class NumberInputWidget : InputWidget<double>
	{
		// Fixed-point so the text never needs an exponent, which the decimal content type can't type
		private const string DEFAULT_FORMAT = "0.######";
		private const string WHOLE_FORMAT = "0";
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
			if (m_wholeNumbers) {
				// Fails on partial input like "" or "-", and on overflow
				var parsed = long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole);
				value = whole;
				return parsed;
			}

			// The decimal content type accepts either separator
			text = text.Replace(',', '.');
			return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}

		protected override string Format(double value)
		{
			var format = m_format ?? (m_wholeNumbers ? WHOLE_FORMAT : DEFAULT_FORMAT);
			return value.ToString(format, CultureInfo.InvariantCulture);
		}

		private void ApplyContentType()
		{
			m_input.contentType = m_wholeNumbers
				? TMP_InputField.ContentType.IntegerNumber
				: TMP_InputField.ContentType.DecimalNumber;
		}
	}
}
