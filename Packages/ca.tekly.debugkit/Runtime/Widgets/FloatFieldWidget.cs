using System;
using Tekly.DebugKit.Utils;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class FloatFieldWidget : Widget
	{
		private readonly Action<float> m_setValue;
		private readonly Func<float> m_getValue;

		private readonly FloatField m_floatField;

		public FloatFieldWidget(Container container, string labelText, string classNames, Func<float> getValue, Action<float> setValue)
		{
			m_setValue = setValue;
			m_getValue = getValue;
			
			m_floatField = new FloatField(labelText);
			m_floatField.selectAllOnFocus = false;
			m_floatField.selectAllOnMouseUp = false;
			m_floatField.isDelayed = true;
			m_floatField.SetValueWithoutNotify(m_getValue());
			m_floatField.AddToClassList("dk-input");
			m_floatField.AddClassNames(classNames);

			container.Root.Add(m_floatField);
			
			m_floatField.RegisterValueChangedCallback(_ =>
			{
				m_setValue(m_floatField.value);
			});
		}

		public override void Update()
		{
			if (m_floatField.focusController.focusedElement != m_floatField) {
				m_floatField.value = m_getValue();
			}
		}
	}
}