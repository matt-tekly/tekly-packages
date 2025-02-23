using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class Property<T> : Widget
	{
		private static readonly IEqualityComparer<T> s_defaultEqualityComparer = EqualityComparer<T>.Default;
		
		private readonly Func<T> m_getValue;
		private readonly string m_format;

		private T m_lastValue;
		private readonly Label m_valueLabel;
        
		public Property(Container container, string labelText, Func<T> getValue, string format = "{0}")
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
            
			if (s_defaultEqualityComparer.Equals(m_lastValue, value)) {
				return;
			}
			
			m_lastValue = value;
			m_valueLabel.text = string.Format(m_format, value);
		}
	}
}