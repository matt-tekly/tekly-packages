using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using UnityEngine;

namespace Tekly.DevBoard.Console
{
	/// <summary>
	/// The console's layout, from the console prefab: the output, the hint, the line typed into, and the box
	/// suggestions float in. DevConsole drives it.
	/// </summary>
	public class ConsoleWidget : Widget
	{
		/// <summary>
		/// The scroll view the output is in.
		/// </summary>
		public ScrollViewWidget Scroll => m_scroll;

		public ConsoleOutput Output => m_output;

		/// <summary>
		/// Shows the command's arguments, or what the line is waiting for. Hidden when empty.
		/// </summary>
		public LabelWidget Hint => m_hint;

		/// <summary>
		/// The line typed into.
		/// </summary>
		public TextInputWidget Input => m_input;

		/// <summary>
		/// Floats over the output, left out of the layout. DevConsole sizes and places it.
		/// </summary>
		public ContainerWidget Suggestions => m_suggestions;

		/// <summary>
		/// One label for each suggestion the box can show.
		/// </summary>
		public LabelWidget[] SuggestionLabels => m_suggestionLabels;

		[SerializeField] private ScrollViewWidget m_scroll;
		[SerializeField] private ConsoleOutput m_output;
		[SerializeField] private LabelWidget m_hint;
		[SerializeField] private TextInputWidget m_input;
		[SerializeField] private ContainerWidget m_suggestions;
		[SerializeField] private LabelWidget[] m_suggestionLabels;
	}
}
