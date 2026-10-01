namespace Tekly.DevBoard.Components.Inputs
{
	public class TextInputWidget : InputWidget<string>
	{
		protected override bool TryParse(string text, out string value)
		{
			value = text;
			return true;
		}

		protected override string Format(string value)
		{
			return value ?? string.Empty;
		}
	}
}
