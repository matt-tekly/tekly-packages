using System;
using Tekly.Common.Utils;
using Tekly.Leaf.Elements;
using Tekly.Trellis;
using TMPro;
using UnityEngine;

namespace Tekly.DevBoard.Components.Inputs
{
	/// <summary>
	/// An input field bound to a value of type T through a getter and setter. Subclasses convert between
	/// the field's text and T; text that doesn't parse is never pushed to the setter.
	/// </summary>
	public abstract class InputWidget<T> : Widget
	{
		[SerializeField] protected LeafInputField m_input;
		[SerializeField] private LabelWidget m_label;

		private Binding<T> m_binding;
		private InputMode m_inputMode = InputMode.Delayed;

		private float m_nextUpdateTime;
		private string m_nextUpdate;
		private bool m_committedThisEdit;

		private BindingWatch<T> m_watch;
		private string m_shownText;

		// The field's own max width, put back when it stops being capped
		private bool m_hasFieldDefaults;
		private OptionalFloat m_defaultMaxWidth;

		private const float DEBOUNCE_TIME = 0.33f;

		public void Initialize(string label, Binding<T> binding)
		{
			m_binding = binding;
			m_nextUpdate = null;
			m_committedThisEdit = false;

			if (m_label != null) {
				m_label.Text = label;
				m_label.gameObject.SetActive(!string.IsNullOrEmpty(label));
			}

			Invalidate();

			try {
				RefreshFromSource();
			} catch (Exception) {
				// Don't break the code building the board. Tick retries, and logs and shows the error if it keeps throwing
			}
		}

		/// <summary>
		/// Puts the label in a width group, or none for null, e.g. so short labels inside a row don't take the
		/// width of the segment's label column.
		/// </summary>
		public InputWidget<T> WithLabelWidthGroup(string widthGroup)
		{
			m_label?.WithWidthGroup(widthGroup ?? string.Empty);
			return this;
		}

		/// <summary>
		/// Stops the field growing past this many characters wide. It still shrinks when there's less room. 0 lets
		/// it fill the row again.
		/// </summary>
		public InputWidget<T> WithMaxInputWidth(int characters)
		{
			// The field's box is the text area's parent: the text area is inset from it by the field's padding
			var textArea = m_input.textViewport;

			if (textArea == null || textArea.parent == null || !textArea.parent.TryGetComponent(out LayoutItem layout)) {
				return this;
			}

			if (!m_hasFieldDefaults) {
				m_hasFieldDefaults = true;
				m_defaultMaxWidth = layout.MaxWidth;
			}

			if (characters <= 0) {
				layout.MaxWidth = m_defaultMaxWidth;
				return this;
			}

			// Digits are as wide as any character in the monospace style, and typical in a proportional one
			var textWidth = m_input.textComponent.GetPreferredValues(new string('0', characters)).x;
			var padding = Mathf.Max(0f, -textArea.sizeDelta.x);

			layout.MaxWidth = textWidth + padding;

			return this;
		}

		/// <summary>
		/// The text shown while the field is empty. Subclasses expose it as WithPlaceholder.
		/// </summary>
		protected void SetPlaceholder(string placeholder)
		{
			if (m_input.placeholder != null && m_input.placeholder.TryGetComponent(out TMP_Text placeholderText)) {
				placeholderText.text = placeholder;
			}
		}

		/// <summary>
		/// When typed text reaches the setter. Delayed by default. Subclasses expose it as WithInputMode.
		/// </summary>
		protected void SetInputMode(InputMode inputMode)
		{
			m_inputMode = inputMode;
		}

		/// <summary>
		/// True while the field is being typed in.
		/// </summary>
		public bool IsFocused => m_input.isFocused;

		/// <summary>
		/// The field itself, for adding behaviour to it.
		/// </summary>
		internal LeafInputField InputField => m_input;

		/// <summary>
		/// Puts the keyboard in the field. With selectAll, its text is selected so typing replaces it.
		/// </summary>
		public void Focus(bool selectAll = true)
		{
			m_input.onFocusSelectAll = selectAll;
			m_input.Select();
			m_input.ActivateInputField();
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
		/// The label widget, for subclasses that add behaviour to it. Null when the prefab has none.
		/// </summary>
		protected LabelWidget LabelComponent => m_label;

		/// <summary>
		/// Reads the bound value. Throws if the getter does.
		/// </summary>
		protected bool TryGetValue(out T value)
		{
			if (m_binding == null) {
				value = default;
				return false;
			}

			value = m_binding.Read();
			return true;
		}

		/// <summary>
		/// Pushes a value to the setter straight away, as if it was typed and committed, and shows it.
		/// </summary>
		protected void SetValue(T value)
		{
			m_binding?.Write(value);
			Invalidate();
			RefreshFromSource();
		}

		/// <summary>
		/// Makes the next refresh rewrite the text even if the value hasn't changed, e.g. after a format change.
		/// </summary>
		protected void Invalidate()
		{
			m_watch.Invalidate();
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

		protected override void OnFaulted(Exception exception)
		{
			// Rewrite the text once the getter works again, even if the value matches the one shown before
			Invalidate();
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
				m_binding?.Write(value);
			}
		}

		private void RefreshFromSource()
		{
			if (m_binding == null || !m_watch.Poll(m_binding, out var value)) {
				return;
			}

			m_shownText = Format(value);

			if (m_input.text != m_shownText) {
				m_input.SetTextWithoutNotify(m_shownText);
			}
		}
	}
}
