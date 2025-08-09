using System;
using Tekly.DebugKit.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public enum InputMode
	{
		Immediate,
		Delayed,
		Debounced
	}
	
	public class TextFieldWidget : Widget
	{
		private readonly Action<string> m_setValue;
		private readonly Func<string> m_getValue;
		
		private readonly TextField m_textField;
		private float m_nextUpdateTime;
		private string m_nextUpdate;
		
		private const float DEBOUNCE_TIME = 0.33f;
		
		public TextFieldWidget(Container container, InputMode inputMode, string labelText, string classNames, Func<string> getValue, Action<string> setValue)
		{
			m_setValue = setValue;
			m_getValue = getValue;
			
			m_textField = new TextField(labelText);
			m_textField.selectAllOnFocus = false;
			m_textField.selectAllOnMouseUp = false;
			
			m_textField.SetValueWithoutNotify(m_getValue());
			m_textField.AddToClassList("dk-input");
			m_textField.AddClassNames(classNames);
			
			if (inputMode == InputMode.Delayed) {
				m_textField.isDelayed = true;	
				m_textField.RegisterValueChangedCallback(_ =>
				{
					m_setValue(m_textField.value);
				});
			} else {
				m_textField.RegisterValueChangedCallback(_ => {
					m_nextUpdate = m_textField.value;
					m_nextUpdateTime = Time.realtimeSinceStartup + (inputMode == InputMode.Immediate ? 0 : DEBOUNCE_TIME);
				});
			}
			
			container.Root.Add(m_textField);
		}

		public override void Update()
		{
			if (m_nextUpdate != null && Time.realtimeSinceStartup >= m_nextUpdateTime) {
				m_setValue(m_nextUpdate);
				m_nextUpdate = null;
			}

			if (m_textField.focusController.focusedElement != m_textField) {
				if (m_nextUpdate != null) {
					m_setValue(m_nextUpdate);
					m_nextUpdate = null;
				} else {
					m_textField.value = m_getValue();	
				}
			}
		}
	}
}