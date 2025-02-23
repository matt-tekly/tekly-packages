using System;
using Tekly.DebugKit.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class CheckboxWidget : Widget
	{
		private readonly Action<bool> m_setValue;
		private readonly Func<bool> m_getValue;
		
		private readonly Toggle m_toggle;

		public CheckboxWidget(Container container, string labelText, Func<bool> getValue, Action<bool> setValue)
			: this(container, labelText, null, getValue, setValue)
		{
			
		}

		public CheckboxWidget(Container container, string labelText, string classNames, Func<bool> getValue, Action<bool> setValue)
		{
			m_setValue = setValue;
			m_getValue = getValue;
			
			m_toggle = new Toggle(labelText);
			m_toggle.AddClassNames(classNames);
			m_toggle.value = getValue();
			
			m_toggle.AddToClassList("dk-input");
			m_toggle.AddToClassList("dk-checkbox");
			
			container.Root.Add(m_toggle);
			
			m_toggle.value = m_getValue();
			
			m_toggle.RegisterValueChangedCallback(evt => {
				m_setValue(evt.newValue);
			});
		}

		public override void Update()
		{
			if (m_toggle.focusController.focusedElement != m_toggle) {
				m_toggle.value = m_getValue();	
			}
		}
	}
}