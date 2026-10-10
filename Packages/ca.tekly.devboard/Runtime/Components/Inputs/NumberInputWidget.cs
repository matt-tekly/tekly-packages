using System.Globalization;
using TMPro;
using UnityEngine;

namespace Tekly.DevBoard.Components.Inputs
{
	/// <summary>
	/// An input for a number. With whole numbers on it takes integers only, which is how int inputs are made.
	/// Values are doubles, which hold every int exactly; ContainerWidget.IntInput and FloatInput convert.
	/// </summary>
	public class NumberInputWidget : InputWidget<double>
	{
		// Fixed-point so the text never needs an exponent, which the decimal content type can't type
		private const string DEFAULT_FORMAT = "0.######";
		private const string WHOLE_FORMAT = "0";

		[Tooltip("Integers only. Code sets this for IntInput and FloatInput")]
		[SerializeField] private bool m_wholeNumbers;

		private string m_format;

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
