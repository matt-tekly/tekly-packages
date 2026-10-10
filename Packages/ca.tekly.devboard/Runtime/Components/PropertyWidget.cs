using System;
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
