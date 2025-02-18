using System;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class Property : Widget
	{
		private readonly Func<object> m_getValue;
		private readonly string m_format;

		private object m_lastValue;
		private readonly Label m_valueLabel;
        
		public Property(Container container, string labelText, Func<object> getValue, string format = "{0}")
		{
			m_getValue = getValue;
			m_format = format;
			
			var propertyRoot = new VisualElement();
			propertyRoot.AddToClassList("dk-property");
			
			var label = new Label(labelText);
			label.AddToClassList("dk-property-label");
			propertyRoot.Add(label);
			
			m_valueLabel = new Label("");
			m_valueLabel.selection.isSelectable = true;
			m_valueLabel.AddToClassList("dk-property-value");
			
			propertyRoot.Add(m_valueLabel);
			container.Root.Add(propertyRoot);
		}

		public override void Update()
		{
			var value = m_getValue();
            
			if (m_lastValue != value)
			{
				m_lastValue = value;
				m_valueLabel.text = string.Format(m_format, value);
			}
		}
	}
}