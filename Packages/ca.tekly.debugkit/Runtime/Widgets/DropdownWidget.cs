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

		public DropdownWidget(Container container, List<string> choices, Func<string> getValue, Action<string> setValue,
			string classNames)
		{
			m_getValue = getValue;

			var index = Math.Max(0, choices.IndexOf(getValue.Invoke()));
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

	public class DropdownSearchableWidget : Widget
	{
		private readonly Func<string> m_getValue;
		private readonly Action<string> m_setValue;
		private readonly SearchableDropdown<string> m_dropdownField;
		private string m_lastValue;

		public DropdownSearchableWidget(Container container, List<string> choices, Func<string> getValue,
			Action<string> setValue, string classNames)
		{
			m_getValue = getValue;
			m_setValue = setValue;

			var index = Math.Max(0, choices.IndexOf(getValue.Invoke()));
			m_dropdownField = new SearchableDropdown<string>();
			m_dropdownField.AddToClassList("dk-dropdown");
			m_dropdownField.AddClassNames(classNames);
		
			m_lastValue = choices[index];
			
			m_dropdownField.SetItems(choices, s => s);
			m_dropdownField.Value = choices[0];
			
			m_dropdownField.SelectionChanged += ValueChanged;

			container.Root.Add(m_dropdownField);
		}

		private void ValueChanged(string newValue)
		{
			m_lastValue = newValue;
			m_setValue(newValue);
		}

		public override void Update()
		{
			var value = m_getValue();

			if (value != m_lastValue) {
				m_lastValue = value;
				m_dropdownField.Value = m_lastValue;
			}
		}
	}
}