using System;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class IntFieldWidget: Widget
	{
		private readonly Action<int> m_setValue;
		private readonly Func<int> m_getValue;

		private readonly IntegerField m_intField;

		public IntFieldWidget(Container container, string labelText, Func<int> getValue, Action<int> setValue)
		{
			m_setValue = setValue;
			m_getValue = getValue;
			
			m_intField = new IntegerField(labelText);
			m_intField.selectAllOnFocus = false;
			m_intField.selectAllOnMouseUp = false;
			m_intField.isDelayed = true;
			m_intField.SetValueWithoutNotify(m_getValue());
			m_intField.AddToClassList("dk-input");

			container.Root.Add(m_intField);
			
			m_intField.RegisterValueChangedCallback(_ =>
			{
				m_setValue(m_intField.value);
			});
		}

		public override void Update()
		{
			if (m_intField.focusController.focusedElement != m_intField) {
				m_intField.value = m_getValue();
			}
		}
	}
}