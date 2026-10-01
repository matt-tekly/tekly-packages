using System;
using System.Collections.Generic;
using Tekly.Leaf.Elements;
using TMPro;
using UnityEngine;

namespace Tekly.DevBoard.Components.Inputs
{
	/// <summary>
	/// When an <see cref="InputWidget{T}"/> pushes typed text to its setter.
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

	/// <summary>
	/// An input field bound to a value of type T through a getter and setter. Subclasses convert between
	/// the field's text and T; text that doesn't parse is never pushed to the setter.
	/// </summary>
	public abstract class InputWidget<T> : Widget
	{
		[SerializeField] protected LeafInputField m_input;
		[SerializeField] private LabelWidget m_label;

		private static readonly IEqualityComparer<T> s_comparer = EqualityComparer<T>.Default;

		private Action<T> m_setValue;
		private Func<T> m_getValue;
		private InputMode m_inputMode = InputMode.Delayed;

		private float m_nextUpdateTime;
		private string m_nextUpdate;
		private bool m_committedThisEdit;

		private T m_shownValue;
		private string m_shownText;
		private bool m_hasShownValue;

		private const float DEBOUNCE_TIME = 0.33f;

		public void Initialize(string label, string placeholder, Func<T> getValue, Action<T> setValue,
			InputMode inputMode = InputMode.Delayed)
		{
			m_getValue = getValue;
			m_setValue = setValue;
			m_inputMode = inputMode;
			m_nextUpdate = null;
			m_committedThisEdit = false;

			if (m_label != null) {
				m_label.Text = label;
				m_label.gameObject.SetActive(!string.IsNullOrEmpty(label));
			}

			if (m_input.placeholder != null && m_input.placeholder.TryGetComponent(out TMP_Text placeholderText)) {
				placeholderText.text = placeholder;
			}

			Invalidate();
			RefreshFromSource();
		}

		/// <summary>
		/// Converts the field's text to a value. Return false to reject the text.
		/// </summary>
		protected abstract bool TryParse(string text, out T value);

		/// <summary>
		/// Converts a value to the text shown in the field.
		/// </summary>
		protected abstract string Format(T value);

		/// <summary>
		/// Makes the next refresh rewrite the text even if the value hasn't changed, e.g. after a format change.
		/// </summary>
		protected void Invalidate()
		{
			m_hasShownValue = false;
		}

		protected virtual void Awake()
		{
			m_input.onValueChanged.AddListener(OnValueChanged);
			m_input.onEndEdit.AddListener(OnEndEdit);
		}

		protected virtual void OnDestroy()
		{
			m_input.onValueChanged.RemoveListener(OnValueChanged);
			m_input.onEndEdit.RemoveListener(OnEndEdit);
		}

		protected override void Tick()
		{
			if (m_nextUpdate != null && Time.realtimeSinceStartup >= m_nextUpdateTime) {
				Commit(m_nextUpdate);
				m_committedThisEdit = true;
			}

			// Don't fight the user while they're typing
			if (!m_input.isFocused && m_nextUpdate == null) {
				RefreshFromSource();
			}
		}

		private void OnValueChanged(string text)
		{
			// Only user edits get here: refreshes use SetTextWithoutNotify
			if (m_inputMode == InputMode.Delayed) {
				return;
			}

			m_nextUpdate = text;
			m_nextUpdateTime = Time.realtimeSinceStartup + (m_inputMode == InputMode.Debounced ? DEBOUNCE_TIME : 0f);
		}

		private void OnEndEdit(string text)
		{
			// Commits the final text, which also flushes a pending debounce. Escape restores the original text
			// before this fires, so a cancel writes the original back over anything committed mid-edit.
			// Untouched text isn't committed, so a lossy Format can't change the value by focusing and leaving.
			if (m_committedThisEdit || text != m_shownText) {
				Commit(text);
			}

			m_nextUpdate = null;
			m_committedThisEdit = false;

			// Rewrite the text from the source even if it didn't change: the text may not have parsed,
			// or the setter may have clamped or rejected it
			Invalidate();
		}

		private void Commit(string text)
		{
			m_nextUpdate = null;

			if (TryParse(text, out var value)) {
				m_setValue?.Invoke(value);
			}
		}

		private void RefreshFromSource()
		{
			if (m_getValue == null) {
				return;
			}

			var value = m_getValue();

			if (m_hasShownValue && s_comparer.Equals(value, m_shownValue)) {
				return;
			}

			m_shownValue = value;
			m_shownText = Format(value);
			m_hasShownValue = true;

			if (m_input.text != m_shownText) {
				m_input.SetTextWithoutNotify(m_shownText);
			}
		}
	}
}
