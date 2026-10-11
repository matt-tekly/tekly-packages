using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using Tekly.DevBoard.Nodes;

namespace Tekly.DevBoard.Views
{
	public sealed partial class NodeRenderers
	{
		/// <summary>
		/// A registry with every built-in node registered.
		/// </summary>
		public static NodeRenderers CreateDefault()
		{
			var renderers = new NodeRenderers();
			renderers.RegisterDefaults();

			return renderers;
		}

		private const string DESCRIPTION_STYLE = "sublabel";

		private void RegisterDefaults()
		{
			Register<LabelNode, LabelWidget>((node, parent) => node.Kind == LabelKind.Heading ? WidgetPrefabs.Heading : WidgetPrefabs.Label,
				(node, widget, view) => widget.Text = node.Kind == LabelKind.Error ? ErrorText(node.Text) : node.Text);

			Register<PropertyNode, PropertyWidget>(WidgetPrefabs.Property, (node, widget, view) => {
				node.Accept(new PropertyBinder(widget, LabelText(node), node.Format));
				widget.WithMonospaced(node.Monospaced);
			});

			Register<ButtonNode, ButtonWidget>(WidgetPrefabs.Button,
				(node, widget, view) => widget.Initialize(node.Action, LabelText(node)));

			Register<LinkNode, ButtonWidget>(WidgetPrefabs.PageButton,
				(node, widget, view) => widget.Initialize(() => view.Open(node.Target), LabelText(node)));

			Register<ToggleNode, ToggleWidget>(WidgetPrefabs.Toggle,
				(node, widget, view) => widget.Initialize(LabelText(node), node.Binding));

			Register<TextInputNode, TextInputWidget>(WidgetPrefabs.TextInput, (node, widget, view) => {
				widget.Initialize(LabelText(node), node.Binding);
				widget.WithPlaceholder(node.Placeholder).WithInputMode(node.InputMode);
			});

			Register<NumberInputNode, NumberInputWidget>(WidgetPrefabs.NumberInput, (node, widget, view) => {
				widget.WithWholeNumbers(node.WholeNumbers)
					.WithFormat(node.Format)
					.WithDragStep(node.DragStep)
					.WithDragScrub(node.DragScrub);

				widget.Initialize(LabelText(node), node.Binding);
				widget.WithPlaceholder(node.Placeholder).WithInputMode(node.InputMode);
			});

			Register<SliderNode, SliderWidget>(WidgetPrefabs.Slider, (node, widget, view) => {
				widget.Initialize(LabelText(node), node.Min, node.Max, node.WholeNumbers, node.Binding);
				widget.WithFormat(node.Format)
					.WithValueInput(node.ShowValueInput).WithValueInLabel(node.ShowValueInLabel);
			});

			Register<DropdownNode, DropdownWidget>(WidgetPrefabs.Dropdown, (node, widget, view) => {
				widget.Initialize(LabelText(node), node.GetCount, node.GetChoice, node.Index);
				widget.WithSearch(node.Searchable);
			});

			// A divider runs across its parent's layout axis
			Register<DividerNode, DividerWidget>((node, parent) =>
					parent.Axis == Common.Utils.LayoutAxis.Horizontal ? WidgetPrefabs.DividerVertical : WidgetPrefabs.DividerHorizontal,
				null);

			RegisterFactory<ContainerNode, ContainerWidget>(CreateContainer, (node, widget, view) => ApplyLayout(node, widget));

			Register<FoldoutNode, FoldoutWidget>(WidgetPrefabs.Foldout, (node, widget, view) => {
				widget.Initialize(LabelText(node));
				widget.UseFullStateKey(view.StateKey(node, "expanded"));
				widget.StartExpanded(node.StartExpanded);
				ApplyLayout(node, widget);
			});

			// Scopes, list rows and dynamic scopes just group their content
			RegisterFactory<ScopeNode, ContainerWidget>((node, parent) => ContainerWidget.CreatePlain(parent.Content, "Scope"),
				(node, widget, view) => ApplyLayout(node, widget));

			Register<Segment, ContainerWidget>(WidgetPrefabs.Card, (node, widget, view) => {
				if (node.Title != null) {
					widget.Heading(node.Title);
				}

				ApplyLayout(node, widget);
			});

			RegisterFactory<ListNode, ContainerWidget>((node, parent) => ContainerWidget.CreatePlain(parent.Content, "List"),
				(node, widget, view) => ApplyLayout(node, widget));
		}

		/// <summary>
		/// The text for a widget's label: the node's label, with its description under it in the "sublabel" style.
		/// </summary>
		public static string LabelText(Node node)
		{
			if (string.IsNullOrEmpty(node.Description)) {
				return node.Label;
			}

			var description = $"<style=\"{DESCRIPTION_STYLE}\">{node.Description}</style>";
			return string.IsNullOrEmpty(node.Label) ? description : $"{node.Label}\n{description}";
		}

		public static string ErrorText(string text)
		{
			return $"<color={ERROR_COLOR}>{text}</color>";
		}

		/// <summary>
		/// The prefab for each kind of container, or a bare layout for a stack. A node's Prefab overrides it.
		/// </summary>
		private static ContainerWidget CreateContainer(ContainerNode node, ContainerWidget parent)
		{
			if (node.Style != null) {
				return parent.Create(new WidgetPrefab<ContainerWidget>(node.Style));
			}

			switch (node.Kind) {
				case ContainerKind.Card:
					return parent.Create(WidgetPrefabs.Card);
				case ContainerKind.Form:
					return parent.Create(WidgetPrefabs.Form);
				case ContainerKind.Row:
					return parent.Create(WidgetPrefabs.Row);
				default:
					return ContainerWidget.CreatePlain(parent.Content, "Stack");
			}
		}

		/// <summary>
		/// Applies a container node's layout overrides to its widget.
		/// </summary>
		public static void ApplyLayout(ContainerNode node, ContainerWidget widget)
		{
			var layout = node.Layout;

			if (layout.Padding is { } padding) {
				widget.WithPadding(padding);
			}

			if (layout.Spacing is int spacing) {
				widget.WithSpacing(spacing);
			}

			if (layout.Alignment is { } alignment) {
				widget.WithAlignment(alignment);
			}

			if (layout.CrossAlignment is { } crossAlignment) {
				widget.WithCrossAlignment(crossAlignment);
			}

			if (layout.FlexibleWidth is float flexibleWidth) {
				widget.WithFlexibleWidth(flexibleWidth);
			}

			if (layout.FlexibleHeight is float flexibleHeight) {
				widget.WithFlexibleHeight(flexibleHeight);
			}

			if (layout.MaxHeight is float maxHeight) {
				widget.WithMaxHeight(maxHeight);
			}
		}

		/// <summary>
		/// Hands a property node's binding to its widget with the binding's type.
		/// </summary>
		private sealed class PropertyBinder : IBindingVisitor
		{
			private readonly PropertyWidget m_widget;
			private readonly string m_label;
			private readonly string m_format;

			public PropertyBinder(PropertyWidget widget, string label, string format)
			{
				m_widget = widget;
				m_label = label;
				m_format = format;
			}

			public void Visit<T>(Binding<T> binding)
			{
				m_widget.Initialize(m_label, binding, m_format);
			}
		}
	}
}
