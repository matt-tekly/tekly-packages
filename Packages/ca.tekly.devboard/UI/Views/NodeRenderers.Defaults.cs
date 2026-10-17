using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using Tekly.DevBoard.Nodes;
using Tekly.Trellis;
using UnityEngine;

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

		// The width group a form lines its labels up with
		private const string LABEL_WIDTH_GROUP = "label";

		private static readonly string[] s_vectorAxes = { "X", "Y", "Z" };

		private const string ON_LABEL = "On";
		private const string OFF_LABEL = "Off";

		// The color the input and property prefabs give their labels, for labels made outside them
		private static readonly Color s_fieldLabelColor = new Color32(189, 172, 143, 255);

		private void RegisterDefaults()
		{
			Register<LabelNode, LabelWidget>((node, parent) => node.Kind == LabelKind.Heading ? WidgetPrefabs.Heading : WidgetPrefabs.Label,
				(node, widget, view) => widget.Text = LabelText(node.Kind == LabelKind.Error ? ErrorText(node.Text) : node.Text, node.Description));

			Register<PropertyNode, PropertyWidget>(WidgetPrefabs.Property, (node, widget, view) => {
				node.Accept(new PropertyBinder(widget, LabelText(node), node.Format));
				widget.WithMonospaced(node.Monospaced);
				widget.WithValueWidth(node.ValueWidth);
			});

			Register<ButtonNode, ButtonWidget>(WidgetPrefabs.Button, (node, widget, view) => {
				widget.Initialize(node.Run, LabelText(node));
				// InteractableWhen is applied by the panel to every node, through a CanvasGroup
				widget.WithIcon(node.Icon != null ? DevBoardUI.Instance.GetIcon(node.Icon) : null)
					.WithLabelHidden(node.IsIconOnly)
					.WithTone(DevBoardUI.Instance.GetButtonTone(node.Tone), node.IsPlain);
			});

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

			RegisterFactory<OnOffButtonsNode, ContainerWidget>(
				(node, parent) => parent.Row().WithPadding(0).WithCrossAlignment(CrossAlignment.Center),
				(node, row, view) => {
					FieldLabel(row, LabelText(node));

					// Joined into one bar: a green check for On and a red cross for Off
					var buttons = row.Create(WidgetPrefabs.SegmentedRow);
					OnOffButton(buttons, ON_LABEL, Icons.Check, ButtonTone.Positive, () => node.Run(true));
					OnOffButton(buttons, OFF_LABEL, Icons.Close, ButtonTone.Negative, () => node.Run(false));
				});

			RegisterFactory<Vector3InputNode, ContainerWidget>(
				(node, parent) => parent.Row().WithPadding(0).WithCrossAlignment(CrossAlignment.Center),
				(node, row, view) => BindVector3(node, row));

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

			// The tabs' contents are plain stacks, from the ContainerNode registration
			Register<TabsNode, TabsWidget>(WidgetPrefabs.Tabs, (node, widget, view) => ApplyLayout(node, widget));

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
		/// The label in the form's label column, then an input per axis sharing the rest of the row. The axis labels
		/// stay out of the column, so they're only as wide as their letter.
		/// </summary>
		private static void BindVector3(Vector3InputNode node, ContainerWidget row)
		{
			FieldLabel(row, LabelText(node));

			for (var i = 0; i < s_vectorAxes.Length; i++) {
				var axis = i;
				var binding = node.Binding.Convert(vector => (double) vector[axis], value => {
					var vector = node.Binding.Read();
					vector[axis] = (float) value;
					return vector;
				});

				var input = row.NumberInput(s_vectorAxes[i], binding).WithFormat(node.Format);
				input.WithLabelWidthGroup(null);

				if (input.TryGetComponent(out LayoutItem layoutItem)) {
					layoutItem.FlexibleWidth = 1f;
				}
			}
		}

		/// <summary>
		/// One of OnOffButtons' buttons: its icon on a background in the tone's colors. The label is hidden, but kept
		/// for anything that reads it.
		/// </summary>
		private static void OnOffButton(ContainerWidget parent, string label, string icon, ButtonTone tone,
			System.Action action)
		{
			parent.Button(label, action)
				.WithIcon(DevBoardUI.Instance.GetIcon(icon))
				.WithLabelHidden(true)
				.WithTone(DevBoardUI.Instance.GetButtonTone(tone), false);
		}

		/// <summary>
		/// A label for controls built from several widgets, looking like an input's label: the same color, and in
		/// a form's label column. Nothing for an empty label.
		/// </summary>
		private static void FieldLabel(ContainerWidget row, string label)
		{
			if (string.IsNullOrEmpty(label)) {
				return;
			}

			var labelWidget = row.Label(label);
			labelWidget.TextComponent.color = s_fieldLabelColor;
			labelWidget.WithWidthGroup(LABEL_WIDTH_GROUP);
		}

		/// <summary>
		/// The text for a widget's label: the node's label, with its description under it in the "sublabel" style.
		/// </summary>
		public static string LabelText(Node node)
		{
			return LabelText(node.Label, node.Description);
		}

		/// <summary>
		/// label with description under it in the "sublabel" style.
		/// </summary>
		public static string LabelText(string label, string description)
		{
			if (string.IsNullOrEmpty(description)) {
				return label;
			}

			description = $"<style=\"{DESCRIPTION_STYLE}\">{description}</style>";
			return string.IsNullOrEmpty(label) ? description : $"{label}\n{description}";
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

			if (layout.WidthGroupScope) {
				widget.WithWidthGroupScope();
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
