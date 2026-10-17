using System.Collections.Generic;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using Tekly.DevBoard.Nodes;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.Views
{
	/// <summary>
	/// A node shown as a widget in a panel.
	/// </summary>
	internal class WidgetElement : IViewElement, IInteractableElement
	{
		public Widget Widget { get; }

		public WidgetElement(Widget widget)
		{
			Widget = widget;
		}

		public void SetShown(bool shown)
		{
			if (Widget != null && Widget.gameObject.activeSelf != shown) {
				Widget.gameObject.SetActive(shown);
			}
		}

		/// <summary>
		/// Through a CanvasGroup, which every Selectable inside honours, so buttons, toggles, inputs and the rest all
		/// show their prefab's disabled look without knowing about it. Added only to nodes that need one.
		/// </summary>
		public void SetInteractable(bool interactable)
		{
			if (Widget == null) {
				return;
			}

			if (!Widget.TryGetComponent(out CanvasGroup group)) {
				if (interactable) {
					return;
				}

				group = Widget.gameObject.AddComponent<CanvasGroup>();
			}

			group.interactable = interactable;
		}

		public void Destroy()
		{
			if (Widget != null) {
				ContainerWidget.DestroyWidget(Widget.gameObject);
			}
		}
	}

	/// <summary>
	/// A container node shown as a container widget. Children's widgets go in its content, after anything the
	/// prefab or the renderer put there first, like a segment's heading.
	/// </summary>
	internal class ContainerWidgetElement : WidgetElement, IContainerElement
	{
		/// <summary>
		/// The widget children are created in.
		/// </summary>
		protected ContainerWidget Target { get; private set; }

		private readonly NodeRenderers m_renderers;
		private readonly ViewContext m_context;
		private int m_firstIndex;

		public ContainerWidgetElement(ContainerWidget widget, NodeRenderers renderers, ViewContext context) : base(widget)
		{
			m_renderers = renderers;
			m_context = context;

			SetTarget(widget);
		}

		public bool ChildrenVisible => Target != null && Target.Content.gameObject.activeInHierarchy;

		public void ShowChildren()
		{
			if (Widget is FoldoutWidget { Expanded: false } foldout) {
				foldout.Expanded = true;
			}
		}

		public IViewElement CreateChild(Node child)
		{
			return m_renderers.Render(child, Target, m_context);
		}

		public void Order(IReadOnlyList<IViewElement> children)
		{
			var index = m_firstIndex;

			foreach (var child in children) {
				if (child is not WidgetElement { Widget: { } widget }) {
					continue;
				}

				if (widget.transform.GetSiblingIndex() != index) {
					widget.transform.SetSiblingIndex(index);
				}

				index++;
			}
		}

		protected void SetTarget(ContainerWidget target)
		{
			Target = target;
			m_firstIndex = target.Content.childCount;
		}
	}

	/// <summary>
	/// A tabs node: the tab buttons and the tabs' contents, in a TabsWidget.
	/// </summary>
	internal sealed class TabsWidgetElement : ContainerWidgetElement, ITabsElement
	{
		private readonly TabsWidget m_widget;

		public TabsWidgetElement(TabsWidget widget, NodeRenderers renderers, ViewContext context) : base(widget, renderers, context)
		{
			m_widget = widget;
		}

		public void ShowTabs(IReadOnlyList<string> labels, System.Action<int> select)
		{
			m_widget.SetTabs(labels, select);
		}

		public void ShowSelected(int index)
		{
			m_widget.SetSelected(index);
		}
	}

	/// <summary>
	/// A list node. A searchable one gets a search field over its rows and a line under them saying how the search
	/// went, and with a scroll height its rows scroll on their own.
	/// </summary>
	internal sealed class ListWidgetElement : ContainerWidgetElement, IListElement
	{
		private readonly ListNode m_list;
		private ScrollViewWidget m_scroll;
		private LabelWidget m_summary;

		public ListWidgetElement(ListNode list, ContainerWidget widget, NodeRenderers renderers, ViewContext context)
			: base(widget, renderers, context)
		{
			m_list = list;

			// Without a search the rows go straight in the widget. With one, they go in their own container
			if (!list.IsSearchable) {
				widget.WithSpacing(list.RowSpacing);
			}
		}

		public void ShowSearch(Binding<string> query, string placeholder)
		{
			var widget = (ContainerWidget) Widget;

			widget.TextInput(null, query, WidgetPrefabs.SearchInput)
				.WithPlaceholder(placeholder)
				.WithInputMode(InputMode.Debounced);

			ContainerWidget rows;

			if (m_list.ScrollHeight is float maxHeight) {
				// The frame caps the height; the scroll view inside it shrinks to fit and scrolls the rest
				var frame = ContainerWidget.CreatePlain(widget.Content, "Rows");
				frame.WithMaxHeight(maxHeight);

				m_scroll = frame.ScrollView().WithoutSavedState();

				// Rows are a vertical list whatever the scroll view prefab's content is set up as
				m_scroll.WithAxis(Common.Utils.LayoutAxis.Vertical)
					.WithCrossAlignment(CrossAlignment.Stretch)
					.WithSpacing(m_list.RowSpacing);

				rows = m_scroll;
			} else {
				rows = ContainerWidget.CreatePlain(widget.Content, "Rows", m_list.RowSpacing);
			}

			m_summary = widget.Label(string.Empty);
			m_summary.gameObject.SetActive(false);

			SetTarget(rows);
		}

		public void ShowSummary(ListSummary summary)
		{
			var text = string.Empty;

			if (summary.Shown == 0 && (summary.HasQuery || summary.ShowAllWhenEmpty)) {
				text = summary.HasQuery ? "No matches" : "Nothing to show";
			} else if (summary.Hidden > 0) {
				text = summary.HasQuery ? $"{summary.Hidden} more, keep typing" : $"{summary.Hidden} more, type to search";
			}

			m_summary.Text = text;
			m_summary.gameObject.SetActive(text.Length > 0);
		}

		public void ScrollToStart()
		{
			m_scroll?.RestorePosition(default);
		}
	}
}
