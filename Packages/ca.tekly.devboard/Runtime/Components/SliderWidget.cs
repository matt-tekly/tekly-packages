using System;
using System.Globalization;
using Tekly.Leaf.Elements;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// A slider bound to a float through a getter and setter. Dragging pushes every change to the setter.
	/// With whole numbers on, it snaps to integers, which is how int sliders are made.
	/// </summary>
	public class SliderWidget : Widget
	{
		[SerializeField] private LeafSlider m_slider;
		[SerializeField] private LabelWidget m_label;
		[SerializeField] private LabelWidget m_valueLabel;

		private const string DEFAULT_FORMAT = "0.##";

		private Action<float> m_setValue;
		private Func<float> m_getValue;
		private string m_format = DEFAULT_FORMAT;
		private float m_shownValue = float.NaN;

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

			if (m_label != null) {
				m_label.Text = label;
				m_label.gameObject.SetActive(!string.IsNullOrEmpty(label));
			}

			m_slider.wholeNumbers = wholeNumbers;
			m_slider.minValue = min;
			m_slider.maxValue = max;

			if (wholeNumbers) {
				m_format = "0";
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
			m_format = string.IsNullOrEmpty(format) ? DEFAULT_FORMAT : format;
			m_shownValue = float.NaN;
			ShowValue(m_slider.value);
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
			ShowValue(value);
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

			ShowValue(m_slider.value);
		}

		private void ShowValue(float value)
		{
			if (m_valueLabel == null || value == m_shownValue) {
				return;
			}

			m_shownValue = value;
			m_valueLabel.Text = value.ToString(m_format, CultureInfo.InvariantCulture);
		}
	}
}
