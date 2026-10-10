using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components.Inputs;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class ContainerWidget : Widget
	{
		/// <summary>
		/// When set, adds a level to the saved view state keys of everything inside this container, so the same
		/// widgets in different containers keep separate state. See <see cref="DevBoardState.KeyFor"/>.
		/// </summary>
		public virtual string StateScope { get; set; }

		/// <summary>
		/// Where child widgets go.
		/// </summary>
		public RectTransform Content => m_content;

		[SerializeField] protected RectTransform m_content;
		[SerializeField] private FlowLayout m_layout;

		/// <summary>
		/// A container with no prefab: a bare vertical layout with no background, for grouping widgets.
		/// </summary>
		internal static ContainerWidget CreatePlain(Transform parent, string name, int spacing = 4)
		{
			var gameObject = new GameObject(name, typeof(RectTransform));
			var rectTransform = (RectTransform) gameObject.transform;
			rectTransform.SetParent(parent, false);

			var layout = gameObject.AddComponent<FlowLayout>();
			layout.Axis = Common.Utils.LayoutAxis.Vertical;
			layout.Spacing = spacing;
			layout.CrossAlignment = CrossAlignment.Stretch;

			var container = gameObject.AddComponent<ContainerWidget>();
			container.m_content = rectTransform;
			container.m_layout = layout;

			return container;
		}

		/// <summary>
		/// Destroys every widget in this container.
		/// </summary>
		public void Clear()
		{
			for (var i = m_content.childCount - 1; i >= 0; i--) {
				var child = m_content.GetChild(i).gameObject;

				DestroyWidget(child);
			}
		}

		public ContainerWidget WithPadding(int left, int top, int right, int bottom)
		{
			m_layout.Padding = new Edges(left, right, top, bottom);
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
		
		/// <summary>
		/// Which way children are laid out: Vertical stacks them, Horizontal puts them in a row.
		/// </summary>
		public ContainerWidget WithAxis(Common.Utils.LayoutAxis axis)
		{
			m_layout.Axis = axis;
			return this;
		}

		public ContainerWidget WithAlignment(LayoutAlignment layoutAlignment)
		{
			m_layout.Alignment = layoutAlignment;
			return this;
		}
		
		public ContainerWidget WithCrossAlignment(CrossAlignment crossAlignment)
		{
			m_layout.CrossAlignment = crossAlignment;
			return this;
		}

		/// <summary>
		/// How much of a parent layout's spare width this container takes. 1 fills it.
		/// </summary>
		public ContainerWidget WithFlexibleWidth(float flexible = 1f)
		{
			m_layout.FlexibleWidth = flexible;
			return this;
		}

		/// <summary>
		/// How much of a parent layout's spare height this container takes. 1 fills it.
		/// </summary>
		public ContainerWidget WithFlexibleHeight(float flexible = 1f)
		{
			m_layout.FlexibleHeight = flexible;
			return this;
		}

		/// <summary>
		/// The width this container asks a parent layout for, instead of the width of its content.
		/// </summary>
		public ContainerWidget WithPreferredWidth(float width)
		{
			m_layout.PreferredWidth = width;
			return this;
		}

		/// <summary>
		/// The height this container asks a parent layout for, instead of the height of its content.
		/// </summary>
		public ContainerWidget WithPreferredHeight(float height)
		{
			m_layout.PreferredHeight = height;
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

		/// <summary>
		/// Destroys a widget straight away as far as layout and ticking are concerned: it's deactivated and
		/// unparented now, and the GameObject itself is destroyed at the end of the frame.
		/// </summary>
		internal static void DestroyWidget(GameObject widget)
		{
			if (widget == null) {
				return;
			}

			widget.SetActive(false);
			widget.transform.SetParent(null, false);
			Destroy(widget);
		}

		public LabelWidget Label(string text, string variant = "label")
		{
			var instance = Create<LabelWidget>(variant);
			instance.Text = text;

			return instance;
		}

		/// <summary>
		/// A label styled as a heading, for titling a group of widgets.
		/// </summary>
		public LabelWidget Heading(string text, string variant = "label_heading")
		{
			return Label(text, variant);
		}

		public BreadcrumbWidget Breadcrumb(IReadOnlyList<Crumb> crumbs, Action<string> onSelect, string variant = "breadcrumb")
		{
			var instance = Create<BreadcrumbWidget>(variant);
			instance.Set(crumbs, onSelect);

			return instance;
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
		
		public TextInputWidget SearchInput(string label, string placeholder, Func<string> getValue, Action<string> setValue, InputMode inputMode = InputMode.Delayed)
		{
			return TextInput("textinput_search", label, placeholder, getValue, setValue, inputMode);
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

		public SliderWidget Slider(string label, float min, float max, Func<float> getValue, Action<float> setValue, string variant = "slider")
		{
			var instance = Create<SliderWidget>(variant);
			instance.Initialize(label, min, max, false, getValue, setValue);

			return instance;
		}

		public SliderWidget SliderInt(string label, int min, int max, Func<int> getValue, Action<int> setValue, string variant = "slider")
		{
			var instance = Create<SliderWidget>(variant);
			instance.Initialize(label, min, max, true, () => getValue(), value => setValue(Mathf.RoundToInt(value)));

			return instance;
		}

		/// <summary>
		/// A dropdown over a list of items. choices is read each time the dropdown refreshes or opens, so it can change.
		/// getChoice is what an option shows for an item, e.g. a title and subtitle. A getter value that isn't in
		/// choices shows as no choice.
		/// </summary>
		public DropdownWidget Dropdown<T>(string label, IReadOnlyList<T> choices, Func<T, DropdownChoice> getChoice, Func<T> getValue,
			Action<T> setValue, string variant = "dropdown")
		{
			var comparer = EqualityComparer<T>.Default;

			int IndexOf(T value)
			{
				for (var i = 0; i < choices.Count; i++) {
					if (comparer.Equals(choices[i], value)) {
						return i;
					}
				}

				return -1;
			}

			var instance = Create<DropdownWidget>(variant);
			instance.Initialize(label, () => choices.Count, i => getChoice(choices[i]), () => IndexOf(getValue()),
				i => setValue(choices[i]));

			return instance;
		}

		/// <summary>
		/// A dropdown over a list of items, each shown as a line of text from getText.
		/// </summary>
		public DropdownWidget Dropdown<T>(string label, IReadOnlyList<T> choices, Func<T, string> getText, Func<T> getValue,
			Action<T> setValue, string variant = "dropdown")
		{
			return Dropdown(label, choices, item => new DropdownChoice(getText(item)), getValue, setValue, variant);
		}

		public DropdownWidget Dropdown(string label, IReadOnlyList<string> choices, Func<string> getValue, Action<string> setValue,
			string variant = "dropdown")
		{
			return Dropdown(label, choices, s => s, getValue, setValue, variant);
		}

		/// <summary>
		/// A dropdown over every value of an enum.
		/// </summary>
		public DropdownWidget Dropdown<TEnum>(string label, Func<TEnum> getValue, Action<TEnum> setValue, string variant = "dropdown")
			where TEnum : struct, Enum
		{
			var values = (TEnum[]) Enum.GetValues(typeof(TEnum));
			return Dropdown(label, values, v => v.ToString(), getValue, setValue, variant);
		}
	}
}
