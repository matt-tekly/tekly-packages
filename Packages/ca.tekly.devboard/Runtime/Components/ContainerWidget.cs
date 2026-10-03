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
			widget.transform.SetParent(m_content, false);
		}

		/// <summary>
		/// Creates a widget from the prefab registered under variant, inside this container.
		/// Every widget factory goes through here.
		/// </summary>
		public T Create<T>(string variant) where T : Widget
		{
			return Instantiate(DevBoard.Instance.Get<T>(variant), m_content, false);
		}

		public PropertyWidget Property<T>(string label, Func<T> getValue, string format = "{0}", string variant = "property")
		{
			var instance = Create<PropertyWidget>(variant);
			instance.Initialize(label, getValue, format);

			return instance;
		}

		public PropertyWidget PropertyMonospaced<T>(string label, Func<T> getValue, string format = "{0}", string variant = "property")
		{
			var instance = Property(label, getValue, format, variant);
			instance.Value.Monospaced = true;

			return instance;
		}

		public PropertyWidget Property(string label, Func<float> getValue, float epsilon, string format = "{0}", string variant = "property")
		{
			var instance = Create<PropertyWidget>(variant);
			instance.Initialize(label, getValue, epsilon, format);

			return instance;
		}

		public PropertyWidget PropertyMonospaced(string label, Func<float> getValue, float epsilon, string format = "{0}", string variant = "property")
		{
			var instance = Property(label, getValue, epsilon, format, variant);
			instance.Value.Monospaced = true;

			return instance;
		}

		public ContainerWidget Card(string variant = "container")
		{
			return Create<ContainerWidget>(variant);
		}

		public ContainerWidget Form(string variant = "container_form")
		{
			return Create<ContainerWidget>(variant);
		}

		public ContainerWidget Row(string variant = "container_row")
		{
			return Create<ContainerWidget>(variant);
		}

		public ScrollViewWidget ScrollView(string variant = "scrollview")
		{
			return Create<ScrollViewWidget>(variant);
		}

		public ButtonWidget Button(string label, Action action, string variant = "button")
		{
			var instance = Create<ButtonWidget>(variant);
			instance.Initialize(action, label);

			return instance;
		}

		public FoldoutWidget Foldout(string label, string variant = "foldout")
		{
			var instance = Create<FoldoutWidget>(variant);
			instance.Initialize(label);

			return instance;
		}

		public TextInputWidget TextInput(string variant, string label, string placeholder, Func<string> getValue, Action<string> setValue, InputMode inputMode = InputMode.Delayed)
		{
			var instance = Create<TextInputWidget>(variant);
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
			var instance = Create<IntInputWidget>(variant);
			instance.Initialize(label, placeholder, getValue, setValue, inputMode);

			return instance;
		}

		public IntInputWidget IntInput(string label, Func<int> getValue, Action<int> setValue, InputMode inputMode = InputMode.Delayed)
		{
			return IntInput("intinput", label, null, getValue, setValue, inputMode);
		}

		public FloatInputWidget FloatInput(string variant, string label, string placeholder, Func<float> getValue, Action<float> setValue, InputMode inputMode = InputMode.Delayed)
		{
			var instance = Create<FloatInputWidget>(variant);
			instance.Initialize(label, placeholder, getValue, setValue, inputMode);

			return instance;
		}

		public FloatInputWidget FloatInput(string label, Func<float> getValue, Action<float> setValue, InputMode inputMode = InputMode.Delayed)
		{
			return FloatInput("floatinput", label, null, getValue, setValue, inputMode);
		}

		public DividerWidget Divider()
		{
			// A divider runs across the layout axis
			var variant = m_layout.Axis == Common.Utils.LayoutAxis.Horizontal ? "divider_vertical" : "divider_horizontal";
			return Create<DividerWidget>(variant);
		}

		public ToggleWidget Toggle(string label, Func<bool> getValue, Action<bool> setValue, string variant = "toggle")
		{
			var instance = Create<ToggleWidget>(variant);
			instance.Initialize(label, getValue, setValue);

			return instance;
		}
	}
}
