using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using Tekly.DevBoard.Panels;

namespace Tekly.DevBoard
{
	/// <summary>
	/// The name of a widget prefab registered with DevBoard, typed with the widget it must be. Built-in ones are
	/// in <see cref="WidgetPrefabs"/>; a game adding its own prefabs can declare its own the same way:
	/// <code>public static readonly WidgetPrefab&lt;ButtonWidget&gt; BigButton = "button_big";</code>
	/// Converts from a string, for names that come from data.
	///
	/// The default (no name) means "the usual prefab for this widget" to the factories that take one.
	/// </summary>
	public readonly struct WidgetPrefab<T> where T : Widget
	{
		public string Name { get; }

		public bool IsSet => Name != null;

		public WidgetPrefab(string name)
		{
			Name = name;
		}

		/// <summary>
		/// This prefab, or fallback when this one isn't set.
		/// </summary>
		public WidgetPrefab<T> Or(WidgetPrefab<T> fallback)
		{
			return IsSet ? this : fallback;
		}

		public static implicit operator WidgetPrefab<T>(string name)
		{
			return new WidgetPrefab<T>(name);
		}

		public override string ToString()
		{
			return Name ?? "(default)";
		}
	}

	/// <summary>
	/// The built-in widget prefabs, one for each prefab in DevBoard's assets.
	/// </summary>
	public static class WidgetPrefabs
	{
		public static readonly WidgetPrefab<DevBoardPanel> Panel = "panel";

		public static readonly WidgetPrefab<LabelWidget> Label = "label";
		public static readonly WidgetPrefab<LabelWidget> Heading = "label_heading";
		public static readonly WidgetPrefab<PropertyWidget> Property = "property";
		public static readonly WidgetPrefab<BreadcrumbWidget> Breadcrumb = "breadcrumb";

		public static readonly WidgetPrefab<ContainerWidget> Card = "container";
		public static readonly WidgetPrefab<ContainerWidget> Form = "container_form";
		public static readonly WidgetPrefab<ContainerWidget> Row = "container_row";
		public static readonly WidgetPrefab<ScrollViewWidget> ScrollView = "scrollview";
		public static readonly WidgetPrefab<FoldoutWidget> Foldout = "foldout";
		public static readonly WidgetPrefab<DividerWidget> DividerHorizontal = "divider_horizontal";
		public static readonly WidgetPrefab<DividerWidget> DividerVertical = "divider_vertical";

		public static readonly WidgetPrefab<ButtonWidget> Button = "button";
		public static readonly WidgetPrefab<ButtonWidget> IconButton = "button_icon";
		public static readonly WidgetPrefab<ButtonWidget> PageButton = "button_page";

		public static readonly WidgetPrefab<ToggleWidget> Toggle = "toggle";
		public static readonly WidgetPrefab<SliderWidget> Slider = "slider";
		public static readonly WidgetPrefab<TextInputWidget> TextInput = "textinput";
		public static readonly WidgetPrefab<TextInputWidget> TextInputVertical = "textinput_vertical";
		public static readonly WidgetPrefab<TextInputWidget> SearchInput = "textinput_search";
		public static readonly WidgetPrefab<NumberInputWidget> NumberInput = "numberinput";

		public static readonly WidgetPrefab<DropdownWidget> Dropdown = "dropdown";
		public static readonly WidgetPrefab<DropdownPopup> DropdownPopup = "dropdown_popup";
		public static readonly WidgetPrefab<DropdownOption> DropdownOption = "dropdown_option";
	}
}
