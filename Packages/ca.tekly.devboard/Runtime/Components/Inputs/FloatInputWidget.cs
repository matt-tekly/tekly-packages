using System.Globalization;
using TMPro;

namespace Tekly.DevBoard.Components.Inputs
{
	public class FloatInputWidget : InputWidget<float>
	{
		// Fixed-point so the text never needs an exponent, which the decimal content type can't type
		private const string DEFAULT_FORMAT = "0.######";

		private string m_format = DEFAULT_FORMAT;

		/// <summary>
		/// Sets the numeric format string used to display the value, e.g. "0.00".
		/// </summary>
		public FloatInputWidget WithFormat(string format)
		{
			m_format = string.IsNullOrEmpty(format) ? DEFAULT_FORMAT : format;
			Invalidate();
			return this;
		}

		protected override void Awake()
		{
			base.Awake();
			m_input.contentType = TMP_InputField.ContentType.DecimalNumber;
		}

		protected override bool TryParse(string text, out float value)
		{
			// The decimal content type accepts either separator
			text = text.Replace(',', '.');
			return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}

		protected override string Format(float value)
		{
			return value.ToString(m_format, CultureInfo.InvariantCulture);
		}
	}
}
