using System;
using System.Globalization;
using Tekly.DevBoard.Components.Inputs;
using Tekly.Leaf.Elements;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// A slider bound to a float through a getter and setter. Dragging pushes every change to the setter.
	/// With whole numbers on, it snaps to integers, which is how int sliders are made.
	///
	/// An optional number input beside it shows the value and takes a typed one, clamped to the slider's range.
	/// WithValueInLabel shows the value on a second line under the label instead (or as well), in the "sublabel"
	/// text style.
	/// </summary>
	public class SliderWidget : Widget
	{
		[SerializeField] private LeafSlider m_slider;
		[SerializeField] private LabelWidget m_label;

		[Tooltip("Shows the value and takes a typed one. Optional")]
		[SerializeField] private NumberInputWidget m_valueInput;

		private const string DEFAULT_FORMAT = "0.##";
		private const string WHOLE_FORMAT = "0";
		private const string VALUE_STYLE = "sublabel";

		private Action<float> m_setValue;
		private Func<float> m_getValue;

		private string m_labelText;
		private string m_format = DEFAULT_FORMAT;
		private bool m_valueInLabel;
		private float m_labelValue = float.NaN;

		private void Awake()
		{
			m_slider.onValueChanged.AddListener(OnValueChanged);
		}

		private void OnDestroy()
		{
			m_slider.onValueChanged.RemoveListener(OnValueChanged);
		}

		public void Initialize(string label, float min, float max, bool wholeNumbers, Func<float> getValue, Action<float> setValue)
		{
			m_getValue = getValue;
			m_setValue = setValue;

			m_labelText = label;
			m_format = wholeNumbers ? WHOLE_FORMAT : DEFAULT_FORMAT;
			RefreshLabel(true);

			m_slider.wholeNumbers = wholeNumbers;
			m_slider.minValue = min;
			m_slider.maxValue = max;

			if (m_valueInput != null) {
				m_valueInput.WithWholeNumbers(wholeNumbers).WithFormat(m_format);

				// Delayed, so the slider doesn't jump about while a number is being typed
				m_valueInput.Initialize(null, null, () => m_getValue(), SetTypedValue);
			}

			try {
				Refresh();
			} catch (Exception) {
				// Don't break the code building the board. Tick retries, and logs the error if it keeps throwing
			}
		}

		/// <summary>
		/// Sets the numeric format string used to display the value, e.g. "0.00".
		/// </summary>
		public SliderWidget WithFormat(string format)
		{
			m_format = string.IsNullOrEmpty(format) ? (m_slider.wholeNumbers ? WHOLE_FORMAT : DEFAULT_FORMAT) : format;

			if (m_valueInput != null) {
				m_valueInput.WithFormat(m_format);
			}

			RefreshLabel(true);
			return this;
		}

		/// <summary>
		/// Shows the value on a second line under the label, in the "sublabel" text style, like a dropdown
		/// option's subtitle. Pair it with WithValueInput(false) for a compact slider that still shows its value.
		/// </summary>
		public SliderWidget WithValueInLabel(bool show)
		{
			m_valueInLabel = show;
			RefreshLabel(true);
			return this;
		}

		/// <summary>
		/// Shows or hides the value input beside the slider. Hidden, the slider takes the room.
		/// </summary>
		public SliderWidget WithValueInput(bool show)
		{
			if (m_valueInput != null) {
				m_valueInput.gameObject.SetActive(show);
			}

			return this;
		}

		protected override void Tick()
		{
			Refresh();
		}

		private void OnValueChanged(float value)
		{
			// Only user drags get here: refreshes use SetValueWithoutNotify
			m_setValue?.Invoke(value);
		}

		private void SetTypedValue(double value)
		{
			var clamped = Mathf.Clamp((float) value, m_slider.minValue, m_slider.maxValue);

			if (m_slider.wholeNumbers) {
				clamped = Mathf.Round(clamped);
			}

			m_setValue?.Invoke(clamped);
		}

		private void Refresh()
		{
			if (m_getValue == null) {
				return;
			}

			var value = m_getValue();

			// Without notify, so a change made by the game isn't pushed back through the setter
			if (!Mathf.Approximately(m_slider.value, value)) {
				m_slider.SetValueWithoutNotify(value);
			}

			RefreshLabel(false);
		}

		/// <summary>
		/// Writes the label, with the value under it when shown there. Only rewrites when the value changes,
		/// unless forced.
		/// </summary>
		private void RefreshLabel(bool force)
		{
			if (m_label == null) {
				return;
			}

			if (!m_valueInLabel) {
				if (force) {
					m_label.Text = m_labelText;
					m_label.gameObject.SetActive(!string.IsNullOrEmpty(m_labelText));
				}

				return;
			}

			var value = m_slider.value;

			if (!force && value == m_labelValue) {
				return;
			}

			m_labelValue = value;

			var valueText = $"<style=\"{VALUE_STYLE}\">{value.ToString(m_format, CultureInfo.InvariantCulture)}</style>";
			m_label.Text = string.IsNullOrEmpty(m_labelText) ? valueText : $"{m_labelText}\n{valueText}";
			m_label.gameObject.SetActive(true);
		}
	}
}
