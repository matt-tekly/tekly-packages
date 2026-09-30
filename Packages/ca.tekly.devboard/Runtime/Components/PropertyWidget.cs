using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class PropertyWidget : Widget
	{
		[SerializeField] private LabelWidget m_name;
		[SerializeField] private LabelWidget m_value;
		
		private Property m_property;
		
		public void Initialize<T>(string name, Func<T> getValue, string format)
		{
			m_name.TextComponent.text = name;
			m_property = new Property<T>(this, getValue, format);
		}

		protected override void Tick()
		{
			m_property?.Tick();
		}

		private abstract class Property
		{
			public abstract void Tick();
		}
		
		private class Property<T> : Property
		{
			private static readonly IEqualityComparer<T> s_defaultEqualityComparer = EqualityComparer<T>.Default;
			
			private readonly PropertyWidget m_widget;
			private readonly Func<T> m_getValue;
			private readonly string m_format;
			
			private T m_lastValue;

			public Property(PropertyWidget widget, Func<T> getValue, string format)
			{
				m_widget = widget;
				m_getValue = getValue;
				m_format = format;
				
				try
				{
					var value = m_getValue();
					m_widget.m_value.Text = string.Format(m_format, value);
					m_lastValue = value;
				}
				catch (Exception)
				{
					// Do nothing
				}
			}

			public override void Tick()
			{
				var value = m_getValue();
            
				if (s_defaultEqualityComparer.Equals(m_lastValue, value)) {
					return;
				}
			
				m_lastValue = value;
				m_widget.m_value.Text = string.Format(m_format, value);
			}
		}
		
	}
}