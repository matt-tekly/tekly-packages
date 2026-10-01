using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DevBoard.Components
{
	public class PropertyWidget : Widget
	{
		public LabelWidget Label => m_label;
		public LabelWidget Value => m_value;
		
		[SerializeField] private LabelWidget m_label;
		[SerializeField] private LabelWidget m_value;
		
		private Property m_property;
		
		/// <summary>
		/// Shows the value of getValue, reformatting only when it changes. isSame decides whether a new value
		/// counts as a change from the last one shown; null uses the default equality for T.
		/// </summary>
		public void Initialize<T>(string label, Func<T> getValue, string format, Func<T, T, bool> isSame = null)
		{
			m_label.TextComponent.text = label;
			m_property = new Property<T>(this, getValue, format, isSame);
		}

		/// <summary>
		/// Shows a float, skipping the reformat while it stays within epsilon of the last value shown.
		/// </summary>
		public void Initialize(string label, Func<float> getValue, float epsilon, string format)
		{
			Initialize(label, getValue, format, (last, value) => IsWithin(last, value, epsilon));
		}

		protected override void Tick()
		{
			m_property?.Tick();
		}

		private static bool IsWithin(float last, float value, float epsilon)
		{
			// The == also covers equal infinities, whose difference is NaN
			if (last == value || (float.IsNaN(last) && float.IsNaN(value))) {
				return true;
			}

			return Mathf.Abs(value - last) <= epsilon;
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
			private readonly Func<T, T, bool> m_isSame;
			
			private T m_lastValue;
			private bool m_hasValue;

			public Property(PropertyWidget widget, Func<T> getValue, string format, Func<T, T, bool> isSame)
			{
				m_widget = widget;
				m_getValue = getValue;
				m_format = format;
				m_isSame = isSame ?? s_defaultEqualityComparer.Equals;
				
				try {
					Show(m_getValue());
				} catch (Exception) {
					// Do nothing, Tick retries
				}
			}

			public override void Tick()
			{
				var value = m_getValue();
            
				// Compares against the last value shown, not last frame's, so slow drift still adds up to a change
				if (m_hasValue && m_isSame(m_lastValue, value)) {
					return;
				}
			
				Show(value);
			}

			private void Show(T value)
			{
				m_widget.m_value.Text = string.Format(m_format, value);
				m_lastValue = value;
				m_hasValue = true;
			}
		}
		
	}
}
