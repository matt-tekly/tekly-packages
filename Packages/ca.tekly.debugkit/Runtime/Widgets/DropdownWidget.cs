using System;
using System.Collections.Generic;
using Tekly.DebugKit.Utils;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class DropdownWidget : Widget
	{
		private readonly Func<string> m_getValue;
		private readonly DropdownField m_dropdownField;
		private string m_lastValue;
		
		public DropdownWidget(Container container, List<string> choices, Func<string> getValue, Action<string> setValue, string classNames)
		{
			m_getValue = getValue;
			
			var index = choices.IndexOf(getValue.Invoke());
			m_dropdownField = new DropdownField(choices, index);
			m_dropdownField.AddToClassList("dk-dropdown");
			m_dropdownField.AddClassNames(classNames);
			
			m_lastValue = choices[index];
			
			m_dropdownField.RegisterValueChangedCallback(evt => {
				m_lastValue = evt.newValue;
				setValue(evt.newValue);
			});
			
			container.Root.Add(m_dropdownField);
		}

		public override void Update()
		{
			var value = m_getValue();

			if (value != m_lastValue) {
				m_lastValue = value;
				m_dropdownField.value = m_lastValue;
			}
		}
	}
}