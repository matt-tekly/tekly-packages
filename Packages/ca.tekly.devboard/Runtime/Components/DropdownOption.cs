using Tekly.Trellis;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// One row in a dropdown's popup. Shows a DropdownChoice, and can say how big it needs to be for one without
	/// showing it, which lets the popup size itself to every choice while only building the rows in view.
	///
	/// The default shows the choice in the button's one label: the title, with the subtitle on a second line in
	/// the stylesheet's "sublabel" style. Anything else in the option (padding, an icon beside the text) is
	/// measured once and added to every choice's text size. Override Show and Measure for options that show more.
	/// </summary>
	public class DropdownOption : ButtonWidget
	{
		[Tooltip("TMP style the subtitle line is shown in")]
		[SerializeField] private string m_subtitleStyle = "sublabel";

		// The space the option adds around its text (padding, spacing, icons), measured once
		private Vector2 m_chrome;
		private bool m_calibrated;

		public virtual void Show(in DropdownChoice choice)
		{
			Label = Format(choice);
		}

		/// <summary>
		/// Marks this option as the dropdown's current value, or not, as its LeafButton's On state. The button's
		/// Leaf animator shows it, e.g. a color layer or an
		/// active target (a checkmark) for the On flag. Instant, since options are reused as the list scrolls.
		/// </summary>
		public virtual void SetSelected(bool selected)
		{
			if (Button != null) {
				Button.SetIsOn(selected, true);
			}
		}

		/// <summary>
		/// The size this option needs to show choice. Doesn't change what it shows.
		/// </summary>
		public virtual Vector2 Measure(in DropdownChoice choice)
		{
			if (!m_calibrated) {
				Calibrate();
			}

			return TextSize(choice) + m_chrome;
		}

		/// <summary>
		/// The label text for a choice. Names are shown as written, so a "&lt;" in one isn't read as a tag.
		/// </summary>
		protected string Format(in DropdownChoice choice)
		{
			var title = $"<noparse>{choice.Title}</noparse>";

			if (!choice.HasSubtitle) {
				return title;
			}

			return $"{title}\n<style=\"{m_subtitleStyle}\"><noparse>{choice.Subtitle}</noparse></style>";
		}

		/// <summary>
		/// The size of the label's text alone.
		/// </summary>
		protected Vector2 TextSize(in DropdownChoice choice)
		{
			return LabelComponent != null ? LabelComponent.TextComponent.GetPreferredValues(Format(choice)) : Vector2.zero;
		}

		/// <summary>
		/// Lays the option out once around a sample, to find the space it adds around its text.
		/// </summary>
		private void Calibrate()
		{
			m_calibrated = true;

			var sample = new DropdownChoice("Sample");
			var rect = (RectTransform) transform;
			var layout = GetComponent<LayoutContainer>();
			var label = Label;

			var fitWidth = FitMode.None;
			var fitHeight = FitMode.None;

			if (layout != null) {
				fitWidth = layout.FitWidth;
				fitHeight = layout.FitHeight;
				layout.FitWidth = FitMode.Preferred;
				layout.FitHeight = FitMode.Preferred;
			}

			// Measured as selected, so an indicator that takes up room (a checkmark beside the text) is counted
			Show(sample);
			SetSelected(true);
			LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

			var chrome = rect.rect.size - TextSize(sample);
			m_chrome = new Vector2(Mathf.Max(0f, chrome.x), Mathf.Max(0f, chrome.y));

			if (layout != null) {
				layout.FitWidth = fitWidth;
				layout.FitHeight = fitHeight;
			}

			Label = label;
			SetSelected(false);
		}
	}
}
