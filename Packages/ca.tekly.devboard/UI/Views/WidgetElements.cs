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

		public virtual IViewElement CreateChild(Node child)
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

	/// <summary>
	/// A virtual list: rows scroll in a view, with a spacer above and below the rows on screen standing in for the
	/// rest, so the scrollbar covers every match. Every row is the list's RowHeight tall. A searchable one also gets
	/// the search field over its rows and the line under them, like ListWidgetElement.
	/// </summary>
	internal sealed class VirtualListWidgetElement : ContainerWidgetElement, IVirtualListElement
	{
		// Rows made either side of the ones on screen, so a quick scroll doesn't show gaps before the next refresh
		private const int OVERSCAN = 3;

		private readonly ContainerWidget m_widget;
		private readonly ScrollViewWidget m_scroll;
		private readonly ContainerWidget m_above;
		private readonly ContainerWidget m_below;
		private readonly float m_rowHeight;
		private readonly float m_stride;
		private readonly float m_viewHeight;
		private LabelWidget m_summary;

		public VirtualListWidgetElement(ListNode list, ContainerWidget widget, NodeRenderers renderers, ViewContext context)
			: base(widget, renderers, context)
		{
			m_widget = widget;
			m_rowHeight = list.RowHeight ?? 0f;
			m_stride = m_rowHeight + list.RowSpacing;
			m_viewHeight = list.ScrollHeight ?? m_stride * ListNode.DEFAULT_VIRTUAL_ROWS - list.RowSpacing;

			// The frame caps the height; the scroll view inside it shrinks to fit when there are few matches
			var frame = ContainerWidget.CreatePlain(widget.Content, "Rows");
			frame.WithMaxHeight(m_viewHeight);

			m_scroll = frame.ScrollView().WithoutSavedState();

			// The spacers and rows are placed by the scroll view's layout, so it owns the spacing and no padding
			m_scroll.WithAxis(Common.Utils.LayoutAxis.Vertical)
				.WithCrossAlignment(CrossAlignment.Stretch)
				.WithSpacing(list.RowSpacing)
				.WithPadding(0);

			m_above = ContainerWidget.CreatePlain(m_scroll.Content, "Above");
			m_below = ContainerWidget.CreatePlain(m_scroll.Content, "Below");
			m_above.gameObject.SetActive(false);
			m_below.gameObject.SetActive(false);

			SetTarget(m_scroll);
		}

		public void ShowSearch(Binding<string> query, string placeholder)
		{
			var input = m_widget.TextInput(null, query, WidgetPrefabs.SearchInput)
				.WithPlaceholder(placeholder)
				.WithInputMode(InputMode.Debounced);

			// Over the rows, which were made first
			input.transform.SetSiblingIndex(0);

			m_summary = m_widget.Label(string.Empty);
			m_summary.gameObject.SetActive(false);
		}

		public void ShowSummary(ListSummary summary)
		{
			if (m_summary == null) {
				return;
			}

			var text = string.Empty;

			if (summary.Shown == 0 && (summary.HasQuery || summary.ShowAllWhenEmpty)) {
				text = summary.HasQuery ? "No matches" : "Nothing to show";
			}

			m_summary.Text = text;
			m_summary.gameObject.SetActive(text.Length > 0);
		}

		public void ScrollToStart()
		{
			m_scroll.VerticalOffset = 0f;
		}

		/// <summary>
		/// Rows are held to the row height, so the spacers' sums stay true.
		/// </summary>
		public override IViewElement CreateChild(Node child)
		{
			var element = base.CreateChild(child);

			if (element is WidgetElement { Widget: ContainerWidget row }) {
				row.WithPreferredHeight(m_rowHeight).WithMaxHeight(m_rowHeight);
			}

			return element;
		}

		public void GetVisibleRange(int count, out int first, out int end)
		{
			var offset = Mathf.Max(0f, m_scroll.VerticalOffset);
			var view = m_scroll.ViewportHeight;

			// Not laid out yet: assume the view is as tall as it can be
			if (view <= 0f) {
				view = m_viewHeight;
			}

			first = Mathf.Clamp(Mathf.FloorToInt(offset / m_stride) - OVERSCAN, 0, count);
			end = Mathf.Clamp(Mathf.CeilToInt((offset + view) / m_stride) + OVERSCAN, first, count);
		}

		public void ShowRows(int count, int first, IReadOnlyList<IViewElement> rows)
		{
			var shownEnd = first + rows.Count;
			var index = 0;

			// Each spacer is the rows it stands in for, less the gap the layout puts after it
			ShowSpacer(m_above, first, ref index);

			foreach (var row in rows) {
				if (row is WidgetElement { Widget: { } widget }) {
					widget.transform.SetSiblingIndex(index++);
				}
			}

			ShowSpacer(m_below, count - shownEnd, ref index);
		}

		/// <summary>
		/// Centres the row in the view, unless it's already all on screen.
		/// </summary>
		public void ScrollToRow(int index)
		{
			var top = index * m_stride;
			var offset = m_scroll.VerticalOffset;
			var view = m_scroll.ViewportHeight > 0f ? m_scroll.ViewportHeight : m_viewHeight;

			if (top >= offset && top + m_rowHeight <= offset + view) {
				return;
			}

			m_scroll.VerticalOffset = top - (view - m_rowHeight) * 0.5f;
		}

		private void ShowSpacer(ContainerWidget spacer, int rows, ref int index)
		{
			var shown = rows > 0;
			spacer.gameObject.SetActive(shown);

			if (shown) {
				spacer.WithPreferredHeight(rows * m_stride - (m_stride - m_rowHeight));
			}

			spacer.transform.SetSiblingIndex(index++);
		}
	}
}
