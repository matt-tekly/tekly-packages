using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class FloatFieldWidget : Widget
	{
		private readonly Action<float> m_setValue;
		private readonly Func<float> m_getValue;

		private readonly FloatField m_floatField;

		public FloatFieldWidget(Container container, string labelText, Func<float> getValue, Action<float> setValue)
		{
			m_setValue = setValue;
			m_getValue = getValue;

			m_floatField = new FloatField(labelText);
			m_floatField.selectAllOnFocus = false;
			m_floatField.selectAllOnMouseUp = false;
			m_floatField.SetValueWithoutNotify(m_getValue());
			m_floatField.AddToClassList("dk-input");

			container.Root.Add(m_floatField);

			m_floatField.RegisterCallback<KeyDownEvent>(evt => {
				if (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return) {
					m_setValue(m_floatField.value);
				}
			});

			m_floatField.RegisterCallback<FocusOutEvent>(_ => {
				m_floatField.SetValueWithoutNotify(m_getValue());
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