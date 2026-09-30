using System;
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
		
		public DividerWidget Divider()
		{
			var prefab = DevBoard.Instance.GetDivider(m_layout.Axis == Common.Utils.LayoutAxis.Horizontal ? "divider_vertical" : "divider_horizontal");
			var instance = Instantiate(prefab, m_content, false);
			return instance;
		}
	}
}