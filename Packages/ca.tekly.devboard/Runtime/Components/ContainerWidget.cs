using System;
using Tekly.Common.Utils;
using Tekly.DevBoard.Components.Inputs;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class ContainerWidget : Widget
	{
		[SerializeField] protected RectTransform m_content;
		[SerializeField] private FlowLayout m_layout;

		public ContainerWidget WithPadding(int left, int top, int right, int bottom)
		{
			m_layout.Padding = new Edges(left, top, right, bottom);
			return this;
		}
		
		public ContainerWidget WithPadding(int all)
		{
			m_layout.Padding = new Edges(all, all, all, all);
			return this;
		}
		
		public ContainerWidget WithSpacing(int spacing)
		{
			m_layout.Spacing = spacing;
			return this;
		}

		public ContainerWidget WithMaxHeight(float maxHeight)
		{
			m_layout.MaxHeight = maxHeight;
			return this;
		}
		
		public void Add(Widget widget)
		{
			widget.transform.SetParent(m_content);
		}

		public PropertyWidget Property<T>(string label, Func<T> getValue, string format = "{0}")
		{
			var instance = Instantiate(DevBoard.Instance.GetProperty("property"), m_content, false);
			
			instance.Initialize(label, getValue, format);
			
			return instance;
		}
		
		public PropertyWidget PropertyMonospaced<T>(string label, Func<T> getValue, string format = "{0}")
		{
			var instance = Instantiate(DevBoard.Instance.GetProperty("property"), m_content, false);
			
			instance.Initialize(label, getValue, format);
			instance.Value.Monospaced = true;
			
			return instance;
		}
		
		public PropertyWidget Property(string label, Func<float> getValue, float epsilon, string format = "{0}")
		{
			var instance = Instantiate(DevBoard.Instance.GetProperty("property"), m_content, false);
			
			instance.Initialize(label, getValue, epsilon, format);
			
			return instance;
		}
		
		public PropertyWidget PropertyMonospaced(string label, Func<float> getValue, float epsilon, string format = "{0}")
		{
			var instance = Property(label, getValue, epsilon, format);
			instance.Value.Monospaced = true;
			
			return instance;
		}
		
		public ContainerWidget Card()
		{
			var instance = Instantiate(DevBoard.Instance.GetContainer("container"), m_content, false);
			return instance;
		}
		
		public ContainerWidget Form()
		{
			var instance = Instantiate(DevBoard.Instance.GetContainer("container_form"), m_content, false);
			return instance;
		}
		
		public ContainerWidget Row()
		{
			var instance = Instantiate(DevBoard.Instance.GetContainer("container_row"), m_content, false);
			return instance;
		}
		
		public ScrollViewWidget ScrollView()
		{
			var instance = Instantiate(DevBoard.Instance.GetScrollView("scrollview"), m_content, false);
			return instance;
		}
		
		public ButtonWidget Button(string label, Action action)
		{
			var instance = Instantiate(DevBoard.Instance.GetButton("button"), m_content, false);
			instance.Initialize(action, label);
			
			return instance;
		}
		
		public FoldoutWidget Foldout(string label)
		{
			var instance = Instantiate(DevBoard.Instance.GetFoldout("foldout"), m_content, false);
			instance.Initialize(label);
			
			return instance;
		}
		
		public TextInputWidget TextInput(string variant, string label, string placeholder, Func<string> getValue, Action<string> setValue, InputMode inputMode = InputMode.Delayed)
		{
			var instance = Instantiate(DevBoard.Instance.GetTextInput(variant), m_content, false);
			instance.Initialize(label, placeholder, getValue, setValue, inputMode);
			
			return instance;
		}
		
		public TextInputWidget TextInput(string label, string placeholder, Func<string> getValue, Action<string> setValue, InputMode inputMode = InputMode.Delayed)
		{
			return TextInput("textinput", label, placeholder, getValue, setValue, inputMode);
		}

		public TextInputWidget TextInput(string label, Func<string> getValue, Action<string> setValue, InputMode inputMode = InputMode.Delayed)
		{
			return TextInput(label, null, getValue, setValue, inputMode);
		}
		
		public TextInputWidget TextInput(Func<string> getValue, Action<string> setValue, InputMode inputMode = InputMode.Delayed)
		{
			return TextInput(null, null, getValue, setValue, inputMode);
		}
		
		public IntInputWidget IntInput(string variant, string label, string placeholder, Func<int> getValue, Action<int> setValue, InputMode inputMode = InputMode.Delayed)
		{
			var instance = Instantiate(DevBoard.Instance.GetIntInput(variant), m_content, false);
			instance.Initialize(label, placeholder, getValue, setValue, inputMode);
			
			return instance;
		}
		
		public IntInputWidget IntInput(string label, Func<int> getValue, Action<int> setValue, InputMode inputMode = InputMode.Delayed)
		{
			return IntInput("intinput", label, null, getValue, setValue, inputMode);
		}
		
		public FloatInputWidget FloatInput(string variant, string label, string placeholder, Func<float> getValue, Action<float> setValue, InputMode inputMode = InputMode.Delayed)
		{
			var instance = Instantiate(DevBoard.Instance.GetFloatInput(variant), m_content, false);
			instance.Initialize(label, placeholder, getValue, setValue, inputMode);
			
			return instance;
		}
		
		public FloatInputWidget FloatInput(string label, Func<float> getValue, Action<float> setValue, InputMode inputMode = InputMode.Delayed)
		{
			return FloatInput("floatinput", label, null, getValue, setValue, inputMode);
		}
		
		public DividerWidget Divider()
		{
			var prefab = DevBoard.Instance.GetDivider(m_layout.Axis == Common.Utils.LayoutAxis.Horizontal ? "divider_vertical" : "divider_horizontal");
			var instance = Instantiate(prefab, m_content, false);
			return instance;
		}

		public ToggleWidget Toggle(string label, Func<bool> getValue, Action<bool> setValue)
		{
			var prefab = DevBoard.Instance.GetToggle("toggle");
			var instance = Instantiate(prefab, m_content, false);
			
			instance.Initialize(label, getValue, setValue);
			return instance;
		}
	}
}