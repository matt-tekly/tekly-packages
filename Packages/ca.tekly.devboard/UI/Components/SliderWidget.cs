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

		private const string VALUE_STYLE = "sublabel";

		private Binding<float> m_binding;
		private BindingWatch<float> m_watch;

		private string m_labelText;
		private string m_format = NumberText.SLIDER_FORMAT;
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

		public void Initialize(string label, float min, float max, bool wholeNumbers, Binding<float> binding)
		{
			m_binding = binding;
			m_watch.Invalidate();

			m_labelText = label;
			m_format = NumberText.SliderFormat(null, wholeNumbers);
			RefreshLabel(true);

			m_slider.wholeNumbers = wholeNumbers;
			m_slider.minValue = min;
			m_slider.maxValue = max;

			if (m_valueInput != null) {
				m_valueInput.WithWholeNumbers(wholeNumbers).WithFormat(m_format);

				// Delayed, so the slider doesn't jump about while a number is being typed
				m_valueInput.Initialize(null, binding.Convert(value => (double) value, ClampTyped));
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
			m_format = NumberText.SliderFormat(format, m_slider.wholeNumbers);

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
			m_binding?.Write(value);

			// The setter may have clamped or rejected the value, so the next refresh moves the handle to the real one
			m_watch.Invalidate();
		}

		/// <summary>
		/// A typed value, clamped to the slider's range.
		/// </summary>
		private float ClampTyped(double value)
		{
			return NumberText.ClampSlider(value, m_slider.minValue, m_slider.maxValue, m_slider.wholeNumbers);
		}

		private void Refresh()
		{
			if (m_binding == null) {
				return;
			}

			// Without notify, so a change made by the game isn't pushed back through the setter
			if (m_watch.Poll(m_binding, out var value)) {
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

			var valueText = $"<style=\"{VALUE_STYLE}\">{NumberText.FormatSlider(value, m_slider.wholeNumbers, m_format)}</style>";
			m_label.Text = string.IsNullOrEmpty(m_labelText) ? valueText : $"{m_labelText}\n{valueText}";
			m_label.gameObject.SetActive(true);
		}
	}

	public static class SliderWidgetFactory
	{
		public static SliderWidget Slider(this ContainerWidget parent, string label, float min, float max, Func<float> getValue,
			Action<float> setValue, WidgetPrefab<SliderWidget> prefab = default)
		{
			return parent.Slider(label, min, max, new Binding<float>(getValue, setValue), prefab);
		}

		public static SliderWidget Slider(this ContainerWidget parent, string label, float min, float max, Binding<float> binding,
			WidgetPrefab<SliderWidget> prefab = default)
		{
			var instance = parent.Create(prefab.Or(WidgetPrefabs.Slider));
			instance.Initialize(label, min, max, false, binding);

			return instance;
		}

		/// <summary>
		/// A slider that snaps to whole numbers.
		/// </summary>
		public static SliderWidget SliderInt(this ContainerWidget parent, string label, int min, int max, Func<int> getValue,
			Action<int> setValue, WidgetPrefab<SliderWidget> prefab = default)
		{
			var instance = parent.Create(prefab.Or(WidgetPrefabs.Slider));
			var binding = new Binding<int>(getValue, setValue).Convert(value => (float) value, Mathf.RoundToInt);
			instance.Initialize(label, min, max, true, binding);

			return instance;
		}
	}
}
