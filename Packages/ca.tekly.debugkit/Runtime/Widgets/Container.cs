﻿using System;
using System.Collections.Generic;
using Tekly.DebugKit.Utils;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class Container : Widget
	{
		public bool Enabled {
			get => m_enabled;
			set {
				m_enabled = value;
				Root.style.display = m_enabled ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}
		
		public readonly VisualElement Root;
		
		private readonly List<Widget> m_widgets = new List<Widget>();
		private readonly VisualElement m_parent;

		private bool m_enabled = true;
		
		public Container(VisualElement root, string classNames = null)
		{
			Root = new VisualElement();
			Root.AddClassNames(classNames);

			root.Add(Root);
		}

		public Container(VisualElement root, string classNames, string extraClassNames = null)
		{
			Root = new VisualElement();
			Root.AddClassNames(classNames);
			Root.AddClassNames(extraClassNames);

			root.Add(Root);
		}

		public override void Update()
		{
			if (!Enabled) {
				return;
			}
			
			foreach (var widget in m_widgets) {
				widget.Update();
			}
		}

		public Container Heading(string text)
		{
			Label label = new Label(text);
			label.AddToClassList("dk-heading");

			Root.Add(label);
			return this;
		}

		public Container Label(string text)
		{
			Label label = new Label(text);
			label.AddToClassList("dk-label");
			Root.Add(label);

			return this;
		}

		public Container Property<T>(string labelText, Func<T> getValue, string format = "{0}")
		{
			var property = new Property<T>(this, labelText, getValue, format);
			m_widgets.Add(property);

			return this;
		}

		public Container TextField(string label, Func<string> getValue, Action<string> setValue)
		{
			var textField = new TextFieldWidget(this, label, getValue, setValue);
			m_widgets.Add(textField);

			return this;
		}

		public Container FloatField(string label, Func<float> getValue, Action<float> setValue)
		{
			var textField = new FloatFieldWidget(this, label, getValue, setValue);
			m_widgets.Add(textField);

			return this;
		}

		public Container IntField(string label, Func<int> getValue, Action<int> setValue)
		{
			var textField = new IntFieldWidget(this, label, getValue, setValue);
			m_widgets.Add(textField);

			return this;
		}

		public Container SliderFloat(string label, float min, float max, Func<float> getValue, Action<float> setValue)
		{
			var slider = new SliderFloatWidget(this, label, min, max, getValue, setValue);
			m_widgets.Add(slider);

			return this;
		}

		public Container SliderFloat(float min, float max, Func<float> getValue, Action<float> setValue)
		{
			return SliderFloat(null, min, max, getValue, setValue);
		}

		public Container SliderInt(string label, int min, int max, Func<int> getValue, Action<int> setValue)
		{
			var slider = new SliderIntWidget(this, label, min, max, getValue, setValue);
			m_widgets.Add(slider);

			return this;
		}

		public Container SliderInt(int min, int max, Func<int> getValue, Action<int> setValue)
		{
			return SliderInt(null, min, max, getValue, setValue);
		}

		public Container Button(string label, Action action, Func<bool> isInteractable = null)
		{
			var button = new ButtonWidget(this, label, action, isInteractable);
			m_widgets.Add(button);

			return this;
		}

		public Container Button(string label, string classNames, Action action, Func<bool> isInteractable = null)
		{
			var button = new ButtonWidget(this, label, classNames, action, isInteractable);
			m_widgets.Add(button);

			return this;
		}

		public Container Checkbox(string label, string classNames, Func<bool> getValue, Action<bool> setValue)
		{
			var button = new CheckboxWidget(this, label, classNames, getValue, setValue);
			m_widgets.Add(button);

			return this;
		}

		public Container Checkbox(string label, Func<bool> getValue, Action<bool> setValue)
		{
			return Checkbox(label, null, getValue, setValue);
		}

		public Container Row(string classNames = null)
		{
			var container = new Container(Root, "dk-layout-row", classNames);
			m_widgets.Add(container);

			return container;
		}

		public Container Row(string className, Action<Container> builder)
		{
			var container = new Container(Root, "dk-layout-row", className);
			m_widgets.Add(container);

			builder(container);

			return this;
		}

		public Container Row(Action<Container> builder)
		{
			var container = new Container(Root, "dk-layout-row");
			m_widgets.Add(container);

			builder(container);

			return this;
		}

		public Container Column(string classNames = null)
		{
			var container = new Container(Root, "dk-layout-column", classNames);
			m_widgets.Add(container);

			return container;
		}
		
		public Container Column(Action<Container> builder)
		{
			return Column(null, builder);
		}
		
		public Container Column(string className, Action<Container> builder)
		{
			var container = new Container(Root, "dk-layout-column", className);
			m_widgets.Add(container);

			builder(container);

			return this;
		}
		
		public Container CardColumn(Action<Container> builder)
		{
			var container = new Container(Root, "dk-layout-column raised p4 r4 mv4");
			m_widgets.Add(container);

			builder(container);

			return this;
		}

		public Container Separator(string classNames = null)
		{
			VisualElement visualElement = new VisualElement();
			visualElement.AddToClassList("dk-horizontal-separator");
			visualElement.AddClassNames(classNames);

			Root.Add(visualElement);

			return this;
		}
		
		public Container HorizontalSpace(string classNames = null)
		{
			VisualElement visualElement = new VisualElement();
			visualElement.AddToClassList("dk-horizontal-space");
			visualElement.AddClassNames(classNames);

			Root.Add(visualElement);

			return this;
		}

		public Container Foldout(string name, Action<Container> builder)
		{
			return Foldout(name, false, builder);
		}

		public Container Foldout(string name, bool folded, Action<Container> builder)
		{
			var foldout = new Foldout();
			foldout.text = name;
			foldout.value = folded;

			var container = new Container(foldout);
			builder.Invoke(container);

			m_widgets.Add(container);
			Root.Add(foldout);

			return this;
		}

		public Container Updater(Action action, Func<bool> canUpdate = null)
		{
			m_widgets.Add(new Updater(action, canUpdate));
			return this;
		}

		public Container ScrollView(ScrollViewMode scrollViewMode, Action<Container> builder)
		{
			var scrollView = new ScrollView(scrollViewMode);
			var container = new Container(scrollView);
			builder.Invoke(container);

			m_widgets.Add(container);
			Root.Add(scrollView);

			return this;
		}

		public Container Dropdown(List<string> choices, int initialIndex, Action<int> onIndexChanged)
		{
			var dropdown = new DropdownField(choices, initialIndex);
			dropdown.AddToClassList("dk-dropdown");
			dropdown.RegisterValueChangedCallback(evt => onIndexChanged?.Invoke(choices.IndexOf(evt.newValue)));
			Root.Add(dropdown);
			return this;
		}

		public Container Raw(VisualElement rawElement)
		{
			Root.Add(rawElement);
			return this;
		}

		public Container FlexibleSpace(string classNames = null)
		{
			var spacer = new VisualElement();
			spacer.AddClassNames("dk-flexible-space");
			spacer.AddClassNames(classNames);

			Raw(spacer);

			return this;
		}
	}
}