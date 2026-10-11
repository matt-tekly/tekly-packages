using System;
using System.Globalization;

namespace Tekly.DevBoard
{
	/// <summary>
	/// How numbers are typed and shown in DevBoard, the same everywhere: in number inputs and sliders, and when
	/// read or set as text through NodeAccess. Always the invariant culture.
	/// </summary>
	public static class NumberText
	{
		// Fixed-point so the text never needs an exponent, which a decimal input field can't type
		public const string NUMBER_FORMAT = "0.######";
		public const string SLIDER_FORMAT = "0.##";
		public const string WHOLE_FORMAT = "0";

		/// <summary>
		/// Reads a typed number: an integer for whole numbers, otherwise a decimal with either separator. Fails on
		/// partial input like "" or "-", and on overflow.
		/// </summary>
		public static bool TryParse(string text, bool wholeNumbers, out double value)
		{
			if (text == null) {
				value = 0;
				return false;
			}

			if (wholeNumbers) {
				var parsed = long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole);
				value = whole;
				return parsed;
			}

			// Decimal input fields accept either separator
			text = text.Replace(',', '.');
			return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}

		/// <summary>
		/// A number input's value as text. format is null for the default.
		/// </summary>
		public static string FormatNumber(double value, bool wholeNumbers, string format)
		{
			return value.ToString(Resolve(format, wholeNumbers, NUMBER_FORMAT), CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// A slider's value as text. format is null for the default.
		/// </summary>
		public static string FormatSlider(float value, bool wholeNumbers, string format)
		{
			return value.ToString(SliderFormat(format, wholeNumbers), CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// The format a slider shows its value with.
		/// </summary>
		public static string SliderFormat(string format, bool wholeNumbers)
		{
			return Resolve(format, wholeNumbers, SLIDER_FORMAT);
		}

		/// <summary>
		/// A value given to a slider some other way than dragging, e.g. typed, as the slider takes it: clamped to its
		/// range, and rounded for whole numbers.
		/// </summary>
		public static float ClampSlider(double value, float min, float max, bool wholeNumbers)
		{
			var clamped = (float) Math.Min(Math.Max(value, min), max);
			return wholeNumbers ? (float) Math.Round(clamped) : clamped;
		}

		private static string Resolve(string format, bool wholeNumbers, string decimalFormat)
		{
			if (!string.IsNullOrEmpty(format)) {
				return format;
			}

			return wholeNumbers ? WHOLE_FORMAT : decimalFormat;
		}
	}
}
