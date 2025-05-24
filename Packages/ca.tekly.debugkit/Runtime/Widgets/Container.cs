using System;
using System.Collections.Generic;
using System.Linq;
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
		
		public Container(VisualElement parent, string classNames = null)
		{
			Root = new VisualElement();
			Root.userData = this;
			
			Root.AddClassNames(classNames);

			parent.Add(Root);
		}

		public Container(VisualElement parent, string classNames, string extraClassNames = null)
		{
			Root = new VisualElement();
			Root.userData = this;
			
			Root.AddClassNames(classNames);
			Root.AddClassNames(extraClassNames);

			parent.Add(Root);
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

		public Container Heading(string text, string classNames = null)
		{
			Label label = new Label(text);
			label.AddToClassList("dk-heading");
			label.AddClassNames(classNames);
			
			Root.Add(label);
			return this;
		}

		public Container Label(string text, string classNames = null)
		{
			Label label = new Label(text);
			label.AddToClassList("dk-label");
			label.AddClassNames(classNames);
			
			Root.Add(label);

			return this;
		}

		public Container Property<T>(string labelText, Func<T> getValue, string format = "{0}")
		{
			return Property(labelText, null, getValue, format);
		}
		
		public Container Property<T>(string labelText, string classNames, Func<T> getValue, string format = "{0}")
		{
			var property = new Property<T>(this, labelText, classNames, getValue, format);
			AddWidget(property);

			return this;
		}

		public Container TextField(string label, Func<string> getValue, Action<string> setValue)
		{
			return TextField(label, null, getValue, setValue);
		}
		
		public Container TextField(string label, string classNames, Func<string> getValue, Action<string> setValue)
		{
			var textField = new TextFieldWidget(this, label, classNames, getValue, setValue);
			AddWidget(textField);

			return this;
		}

		public Container FloatField(string label, Func<float> getValue, Action<float> setValue)
		{
			return FloatField(label, null, getValue, setValue);
		}
		
		public Container FloatField(string label, string classNames, Func<float> getValue, Action<float> setValue)
		{
			var textField = new FloatFieldWidget(this, label, classNames, getValue, setValue);
			AddWidget(textField);

			return this;
		}

		public Container IntField(string label, Func<int> getValue, Action<int> setValue)
		{
			return IntField(label, null, getValue, setValue);
		}
		
		public Container IntField(string label, string classNames, Func<int> getValue, Action<int> setValue)
		{
			var textField = new IntFieldWidget(this, label, classNames, getValue, setValue);
			AddWidget(textField);

			return this;
		}

		public Container SliderFloat(string label, float min, float max, Func<float> getValue, Action<float> setValue)
		{
			var slider = new SliderFloatWidget(this, label, min, max, getValue, setValue);
			AddWidget(slider);

			return this;
		}

		public Container SliderFloat(float min, float max, Func<float> getValue, Action<float> setValue)
		{
			return SliderFloat(null, min, max, getValue, setValue);
		}

		public Container SliderInt(string label, int min, int max, Func<int> getValue, Action<int> setValue)
		{
			var slider = new SliderIntWidget(this, label, min, max, getValue, setValue);
			AddWidget(slider);

			return this;
		}

		public Container SliderInt(int min, int max, Func<int> getValue, Action<int> setValue)
		{
			return SliderInt(null, min, max, getValue, setValue);
		}

		public Container Button(string label, Action action, Func<bool> isInteractable = null)
		{
			var button = new ButtonWidget(this, label, action, isInteractable);
			AddWidget(button);

			return this;
		}

		public Container Button(string label, string classNames, Action action, Func<bool> isInteractable = null)
		{
			var button = new ButtonWidget(this, label, classNames, action, isInteractable);
			AddWidget(button);

			return this;
		}
		
		public Container ButtonCopy(Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText("copy", action, isInteractable);
		}
		
		public Container ButtonCopy(string classNames, Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText("copy", classNames, action, isInteractable);
		}
		
		public Container ButtonPaste(Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText("paste", action, isInteractable);
		}
		
		public Container ButtonPaste(string classNames, Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText("paste", classNames, action, isInteractable);
		}
		
		public Container ButtonReturn(Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText("return", action, isInteractable);
		}
		
		public Container ButtonReturn(string classNames, Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText("return", classNames, action, isInteractable);
		}
		
		public Container ButtonTrash(Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText("trash", action, isInteractable);
		}
		
		public Container ButtonTrash(string classNames, Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText("trash", classNames, action, isInteractable);
		}
		
		public Container ButtonUpdate(Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText("update", action, isInteractable);
		}
		
		public Container ButtonUpdate(string classNames, Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText("update", classNames, action, isInteractable);
		}
		
		public Container ButtonNoText(string className, Action action, Func<bool> isInteractable = null)
		{
			var button = new ButtonWidget(this, null, className, action, isInteractable);
			AddWidget(button);

			return this;
		}
		
		public Container ButtonNoText(string className, string classNames, Action action, Func<bool> isInteractable = null)
		{
			return ButtonNoText(className + " " + classNames, action, isInteractable);
		}
		
		public Container ButtonOptions(Action action, Func<bool> isInteractable = null)
		{
			var button = new ButtonWidget(this, null, "options", action, isInteractable);
			AddWidget(button);

			return this;
		}
		
		public Container ButtonOptions(string classNames, Action action, Func<bool> isInteractable = null)
		{
			var button = new ButtonWidget(this, null, "options" + " " + classNames, action, isInteractable);
			AddWidget(button);

			return this;
		}

		public Container Checkbox(string label, string classNames, Func<bool> getValue, Action<bool> setValue)
		{
			var button = new CheckboxWidget(this, label, classNames, getValue, setValue);
			AddWidget(button);

			return this;
		}

		public Container Checkbox(string label, Func<bool> getValue, Action<bool> setValue)
		{
			return Checkbox(label, null, getValue, setValue);
		}
		
		public Container Checkbox(string label, BoolPref boolPref)
		{
			return Checkbox(label, null, () => boolPref.Value, v => boolPref.Value = v);
		}

		public Container Row(string classNames = null)
		{
			var container = new Container(Root, "dk-layout-row", classNames);
			AddWidget(container);

			return container;
		}

		public Container Row(string className, Action<Container> builder)
		{
			var container = new Container(Root, "dk-layout-row", className);
			AddWidget(container);

			builder(container);

			return this;
		}

		public Container Row(Action<Container> builder)
		{
			var container = new Container(Root, "dk-layout-row");
			AddWidget(container);

			builder(container);

			return this;
		}
		
		public Container ButtonRow(Action<Container> builder, string classNames = null)
		{
			var container = new Container(Root, "dk-layout-row button-group");
			container.Root.AddClassNames(classNames);
			
			AddWidget(container);

			builder(container);

			return this;
		}

		public Container Column(string classNames = null)
		{
			var container = new Container(Root, "dk-layout-column", classNames);
			AddWidget(container);

			return container;
		}
		
		public Container Column(Action<Container> builder)
		{
			return Column(null, builder);
		}
		
		public Container Column(string className, Action<Container> builder)
		{
			var container = new Container(Root, "dk-layout-column", className);
			AddWidget(container);

			builder(container);

			return this;
		}
		
		public Container CardColumn(Action<Container> builder)
		{
			var container = new Container(Root, "dk-layout-column raised p4 r4 mv4");
			AddWidget(container);

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
		
		public Container VerticalSpace(string classNames = null)
		{
			VisualElement visualElement = new VisualElement();
			visualElement.AddToClassList("dk-vertical-space");
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

			AddWidget(container);
			Root.Add(foldout);

			return this;
		}

		public Container Updater(Action action, Func<bool> canUpdate = null)
		{
			AddWidget(new Updater(action, canUpdate));
			return this;
		}

		public Container ScrollView(ScrollViewMode scrollViewMode, Action<Container> builder)
		{
			var scrollView = new ScrollView(scrollViewMode);
			var container = new Container(scrollView);
			builder.Invoke(container);

			AddWidget(container);
			Root.Add(scrollView);

			return this;
		}

		public Container Dropdown(List<string> choices, Func<string> getValue, Action<string> setValue)
		{
			return Dropdown(choices, null, getValue, setValue);
		}
		
		public Container Dropdown(List<string> choices, string classNames, Func<string> getValue, Action<string> setValue)
		{
			var dropdownWidget = new DropdownWidget(this, choices, getValue, setValue, classNames);
			AddWidget(dropdownWidget);
			
			return this;
		}
		
		public Container Dropdown<TEnum>(Func<TEnum> getValue, Action<TEnum> setValue)
		{
			var names = Enum.GetNames(typeof(TEnum)).ToList();
			
			string GetStringValue() => getValue().ToString();
			void SetStringValue(string v) => setValue((TEnum)Enum.Parse(typeof(TEnum), v));

			return Dropdown(names, null, GetStringValue, SetStringValue);
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

		public void MenuController(string pref, Action<MenuController> action)
		{
			var menuController = new MenuController(Root, pref);
			action.Invoke(menuController);
			menuController.Enable(true);
			
			AddWidget(menuController);
		}
		
		/// <summary>
		/// You are responsible for enabling the MenuController this returns
		/// </summary>
		public MenuController MenuController(string pref)
		{
			var menuController = new MenuController(Root, pref);
			AddWidget(menuController);

			return menuController;
		}

		public Container Style(Action<IStyle> action)
		{
			action(Root.style);
			return this;
		}

		private void AddWidget(Widget widget)
		{
			m_widgets.Add(widget);
		
			if (m_widgets.Count == 2) {
				m_widgets[0].AddClass("first");
				m_widgets[1].AddClass("last");
			} else if (m_widgets.Count > 2) {
				m_widgets[^2].RemoveClass("last");
				m_widgets[^2].AddClass("middle");
				m_widgets[^1].AddClass("last");
			}
		}

		public void Detach()
		{
			DetachFromContainer(Root.parent);
			Root.parent.Remove(Root);
		}

		private void DetachFromContainer(VisualElement parent)
		{
			if (parent.userData is Container container) {
				container.m_widgets.Remove(this);
			} else if (parent.parent != null) {
				DetachFromContainer(parent.parent);
			}
		}
	}
}