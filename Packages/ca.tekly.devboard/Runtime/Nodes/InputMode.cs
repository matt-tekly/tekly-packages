namespace Tekly.DevBoard.Components.Inputs
{
	/// <summary>
	/// When typed text reaches an input's setter.
	/// </summary>
	public enum InputMode
	{
		/// Every edit, applied on the next refresh.
		Immediate,
		/// Only when editing ends: Enter, clicking away or deselecting.
		Delayed,
		/// Once typing pauses for a moment, and when editing ends.
		Debounced
	}
}
