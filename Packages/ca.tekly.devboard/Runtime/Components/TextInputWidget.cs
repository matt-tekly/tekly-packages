using System;
using Tekly.Leaf.Elements;
using TMPro;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// When a <see cref="TextInputWidget"/> pushes typed text to its setter.
	/// </summary>
	public enum InputMode
	{
		/// Every edit, applied on the next Tick.
		Immediate,
		/// Only when editing ends: Enter, clicking away or deselecting.
		Delayed,
		/// Once typing pauses for a moment, and when editing ends.
		Debounced
	}

	public class TextInputWidget : Widget
	{
		[SerializeField] private LeafInputField m_input;
		[SerializeField] private LabelWidget m_label;

		private Action<string> m_setValue;
		private Func<string> m_getValue;
		private InputMode m_inputMode = InputMode.Delayed;

		private float m_nextUpdateTime;
		private string m_nextUpdate;

		private const float DEBOUNCE_TIME = 0.33f;

		public void Initialize(string label, string placeholder, Func<string> getValue, Action<string> setValue,
			InputMode inputMode = InputMode.Delayed)
		{
			m_getValue = getValue;
			m_setValue = setValue;
			m_inputMode = inputMode;
			m_nextUpdate = null;

			if (m_label != null) {
				m_label.Text = label;
				m_label.gameObject.SetActive(!string.IsNullOrEmpty(label));
			}

			if (m_input.placeholder != null && m_input.placeholder.TryGetComponent(out TMP_Text placeholderText)) {
				placeholderText.text = placeholder;
			}

			RefreshFromSource();
		}

		private void Awake()
		{
			m_input.onValueChanged.AddListener(OnValueChanged);
			m_input.onEndEdit.AddListener(OnEndEdit);
		}

		private void OnDestroy()
		{
			m_input.onValueChanged.RemoveListener(OnValueChanged);
			m_input.onEndEdit.RemoveListener(OnEndEdit);
		}

		protected override void Tick()
		{
			if (m_nextUpdate != null && Time.realtimeSinceStartup >= m_nextUpdateTime) {
				Commit(m_nextUpdate);
			}

			// Don't fight the user while they're typing
			if (!m_input.isFocused && m_nextUpdate == null) {
				RefreshFromSource();
			}
		}

		private void OnValueChanged(string value)
		{
			// Only user edits get here: refreshes use SetTextWithoutNotify
			if (m_inputMode == InputMode.Delayed) {
				return;
			}

			m_nextUpdate = value;
			m_nextUpdateTime = Time.realtimeSinceStartup + (m_inputMode == InputMode.Debounced ? DEBOUNCE_TIME : 0f);
		}

		private void OnEndEdit(string value)
		{
			// Every mode commits the final text, which also flushes a pending debounce.
			// Escape restores the original text before this fires, so a cancel writes the original back.
			Commit(value);
		}

		private void Commit(string value)
		{
			m_nextUpdate = null;
			m_setValue?.Invoke(value);
		}

		private void RefreshFromSource()
		{
			if (m_getValue == null) {
				return;
			}

			var value = m_getValue() ?? string.Empty;

			if (m_input.text != value) {
				m_input.SetTextWithoutNotify(value);
			}
		}
	}
}
