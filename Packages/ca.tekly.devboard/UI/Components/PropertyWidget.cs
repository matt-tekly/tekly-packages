using System;
using Tekly.Common.Utils;
using Tekly.Trellis;
using TMPro;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class PropertyWidget : Widget
	{
		public LabelWidget Label => m_label;
		public LabelWidget Value => m_value;
		
		[SerializeField] private LabelWidget m_label;
		[SerializeField] private LabelWidget m_value;
		
		private Property m_property;

		// The value's own sizing, put back when it stops having a fixed width
		private bool m_hasValueDefaults;
		private OptionalFloat m_defaultMinWidth;
		private OptionalFloat m_defaultPreferredWidth;
		private TextWrappingModes m_defaultWrapping;
		private TextOverflowModes m_defaultOverflow;

		private const string ERROR_TEXT = "<color=#E5534B>error</color>";

		/// <summary>
		/// Shows the bound value, reformatting only when the binding says it changed.
		/// </summary>
		public void Initialize<T>(string label, Binding<T> binding, string format)
		{
			m_label.TextComponent.text = label;
			m_property = new Property<T>(this, binding, format);
		}

		/// <summary>
		/// Shows the value in the monospace text style, so digits don't jitter as they change.
		/// </summary>
		public PropertyWidget WithMonospaced(bool monospaced = true)
		{
			m_value.Monospaced = monospaced;
			return this;
		}

		/// <summary>
		/// Makes the value always as wide as this many characters, cutting longer text off with an ellipsis. 0 fits
		/// the value again. Call after WithMonospaced, since the width is measured in the value's text style.
		/// </summary>
		public PropertyWidget WithValueWidth(int characters)
		{
			var text = m_value.TextComponent;

			if (!m_value.TryGetComponent(out LayoutItem layout)) {
				return this;
			}

			if (!m_hasValueDefaults) {
				m_hasValueDefaults = true;
				m_defaultMinWidth = layout.MinWidth;
				m_defaultPreferredWidth = layout.PreferredWidth;
				m_defaultWrapping = text.textWrappingMode;
				m_defaultOverflow = text.overflowMode;
			}

			if (characters <= 0) {
				layout.MinWidth = m_defaultMinWidth;
				layout.PreferredWidth = m_defaultPreferredWidth;
				text.textWrappingMode = m_defaultWrapping;
				text.overflowMode = m_defaultOverflow;

				return this;
			}

			// Digits are as wide as any character in the monospace style, and typical in a proportional one
			var width = text.GetPreferredValues(new string('0', characters)).x;

			layout.MinWidth = width;
			layout.PreferredWidth = width;
			text.textWrappingMode = TextWrappingModes.NoWrap;
			text.overflowMode = TextOverflowModes.Ellipsis;

			return this;
		}

		protected override void Tick()
		{
			m_property?.Tick();
		}

		protected override void OnFaulted(Exception exception)
		{
			m_value.Text = ERROR_TEXT;

			// Force the next successful Tick to show its value, even if it matches the one shown before the fault
			m_property?.Invalidate();
		}

		private abstract class Property
		{
			public abstract void Tick();
			public abstract void Invalidate();
		}
		
		private class Property<T> : Property
		{
			private readonly PropertyWidget m_widget;
			private readonly Binding<T> m_binding;
			private readonly string m_format;

			private BindingWatch<T> m_watch;

			public Property(PropertyWidget widget, Binding<T> binding, string format)
			{
				m_widget = widget;
				m_binding = binding;
				m_format = format;

				try {
					Tick();
				} catch (Exception) {
					// Tick retries, and logs and shows the error if it keeps throwing
				}
			}

			public override void Tick()
			{
				if (m_watch.Poll(m_binding, out var value)) {
					m_widget.m_value.Text = string.Format(m_format, value);
				}
			}

			public override void Invalidate()
			{
				m_watch.Invalidate();
			}
		}
		
	}

	public static class PropertyWidgetFactory
	{
		/// <summary>
		/// Shows the value of getValue with format, updating as it changes.
		/// </summary>
		public static PropertyWidget Property<T>(this ContainerWidget parent, string label, Func<T> getValue, string format = "{0}",
			WidgetPrefab<PropertyWidget> prefab = default)
		{
			return parent.Property(label, new Binding<T>(getValue), format, prefab);
		}

		/// <summary>
		/// Shows a float, skipping the reformat while it stays within epsilon of the last value shown.
		/// </summary>
		public static PropertyWidget Property(this ContainerWidget parent, string label, Func<float> getValue, float epsilon,
			string format = "{0}", WidgetPrefab<PropertyWidget> prefab = default)
		{
			return parent.Property(label, new Binding<float>(getValue, isSame: BindingCompare.Within(epsilon)), format, prefab);
		}

		public static PropertyWidget Property<T>(this ContainerWidget parent, string label, Binding<T> binding, string format = "{0}",
			WidgetPrefab<PropertyWidget> prefab = default)
		{
			var instance = parent.Create(prefab.Or(WidgetPrefabs.Property));
			instance.Initialize(label, binding, format);

			return instance;
		}
	}
}
