using System;
using Tekly.DebugKit.Utils;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class TextFieldWidget : Widget
	{
		private readonly Action<string> m_setValue;
		private readonly Func<string> m_getValue;
		
		private readonly TextField m_textField;
		
		public TextFieldWidget(Container container, string labelText, string classNames, Func<string> getValue, Action<string> setValue)
		{
			m_setValue = setValue;
			m_getValue = getValue;
			
			m_textField = new TextField(labelText);
			m_textField.selectAllOnFocus = false;
			m_textField.selectAllOnMouseUp = false;
			m_textField.isDelayed = true;
			m_textField.SetValueWithoutNotify(m_getValue());
			m_textField.AddToClassList("dk-input");
			m_textField.AddClassNames(classNames);
			
			container.Root.Add(m_textField);
			
			m_textField.RegisterValueChangedCallback(_ =>
			{
				m_setValue(m_textField.value);
			});
		}

		public override void Update()
		{
			if (m_textField.focusController.focusedElement != m_textField) {
				m_textField.value = m_getValue();	
			}
		}
	}
}