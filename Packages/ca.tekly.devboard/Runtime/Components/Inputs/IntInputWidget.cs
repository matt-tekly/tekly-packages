using System.Globalization;
using TMPro;

namespace Tekly.DevBoard.Components.Inputs
{
	public class IntInputWidget : InputWidget<int>
	{
		protected override void Awake()
		{
			base.Awake();
			m_input.contentType = TMP_InputField.ContentType.IntegerNumber;
		}

		protected override bool TryParse(string text, out int value)
		{
			// Fails on partial input like "" or "-", and on overflow
			return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
		}

		protected override string Format(int value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
	}
}
