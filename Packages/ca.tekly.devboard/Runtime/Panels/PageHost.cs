using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Nodes;
using Tekly.DevBoard.Pages;
using Tekly.DevBoard.Views;
using Tekly.Trellis;

namespace Tekly.DevBoard.Panels
{
	/// <summary>
	/// The pages a panel has visited, rendered into its body. Each page has buttons for its child pages, then its
	/// segments' nodes. Pages are kept while they exist, hidden when another is shown, and follow the page tree as
	/// segments and pages come and go. Remembers each page's scroll position.
	/// </summary>
	internal sealed class PageHost
	{
		public PageNode CurrentPage => m_current?.Page;

		private readonly PageTree m_tree;
		private readonly ScrollViewWidget m_body;
		private readonly NodeRenderers m_renderers;
		private readonly ViewContext m_context;
		private readonly Action<string> m_openPage;
		private readonly Action<string> m_currentPageRemoved;

		private readonly Dictionary<PageNode, PageView> m_views = new();
		private PageView m_current;

		/// <param name="openPage">Called when a page link is clicked.</param>
		/// <param name="currentPageRemoved">Called with the path of the page being shown when it's removed.</param>
		public PageHost(PageTree tree, ScrollViewWidget body, NodeRenderers renderers, ViewContext context, Action<string> openPage,
			Action<string> currentPageRemoved)
		{
			m_tree = tree;
			m_body = body;
			m_renderers = renderers;
			m_context = context;
			m_openPage = openPage;
			m_currentPageRemoved = currentPageRemoved;

			// The host remembers scroll positions per page itself
			m_body.WithoutSavedState();

			m_tree.SegmentAdded += OnSegmentAdded;
			m_tree.SegmentRemoved += OnSegmentRemoved;
			m_tree.PageRemoved += OnPageRemoved;
			m_tree.ChildrenChanged += OnChildrenChanged;
		}

		/// <summary>
		/// Shows page, building it if it hasn't been yet. Returns false if it was already showing.
		/// </summary>
		public bool Show(PageNode page)
		{
			if (m_current != null && m_current.Page == page) {
				return false;
			}

			if (m_current != null) {
				m_current.Scroll = m_body.CapturePosition();
				m_current.Root.gameObject.SetActive(false);
			}

			var view = GetOrBuildView(page);
			view.Root.gameObject.SetActive(true);

			if (view.LinksDirty) {
				BuildLinks(view);
			}

			m_current = view;
			m_body.RestorePosition(view.Scroll);

			return true;
		}

		/// <summary>
		/// Makes node show on the current page, opening foldouts and searching lists on the way, and returns its
		/// widget. Null if it isn't on the current page or can't be shown.
		/// </summary>
		public Widget Reveal(Node node)
		{
			var segment = Segment.Of(node);

			if (m_current == null || segment == null) {
				return null;
			}

			foreach (var segmentView in m_current.Segments) {
				if (segmentView.Segment.Root != segment) {
					continue;
				}

				var mirror = segmentView.Mirror.Reveal(node);
				return (mirror?.Element as WidgetElement)?.Widget;
			}

			return null;
		}

		/// <summary>
		/// Throws away every built page. Nothing is shown until the next Show.
		/// </summary>
		public void Reset()
		{
			foreach (var view in m_views.Values) {
				DisposeView(view);
			}

			m_views.Clear();
			m_current = null;
		}

		/// <summary>
		/// Called every frame. Rebuilds the current page's links if its child pages changed.
		/// </summary>
		public void Update()
		{
			if (m_current != null && m_current.LinksDirty) {
				BuildLinks(m_current);
			}
		}

		/// <summary>
		/// Refreshes the nodes on the current page.
		/// </summary>
		public void Tick()
		{
			m_current?.Tick();
		}

		public void Teardown()
		{
			m_tree.SegmentAdded -= OnSegmentAdded;
			m_tree.SegmentRemoved -= OnSegmentRemoved;
			m_tree.PageRemoved -= OnPageRemoved;
			m_tree.ChildrenChanged -= OnChildrenChanged;

			// Stop following the nodes. The widgets go with the panel's GameObject.
			foreach (var view in m_views.Values) {
				foreach (var segment in view.Segments) {
					segment.Mirror.Dispose(false);
				}
			}

			m_views.Clear();
			m_current = null;
		}

		private PageView GetOrBuildView(PageNode page)
		{
			if (m_views.TryGetValue(page, out var view)) {
				return view;
			}

			var root = ContainerWidget.CreatePlain(m_body.Content, page.IsRoot ? "Page (root)" : $"Page {page.Path}", 8);
			root.WithFlexibleWidth();
			root.StateScope = page.Path;

			var links = ContainerWidget.CreatePlain(root.Content, "Links");

			view = new PageView(page, root, links);
			m_views.Add(page, view);

			BuildLinks(view);

			foreach (var segment in page.Segments) {
				BuildSegment(view, segment);
			}

			return view;
		}

		/// <summary>
		/// The buttons for a page's child pages, or a note when the page is empty.
		/// </summary>
		private void BuildLinks(PageView view)
		{
			view.LinksDirty = false;
			view.Links.Clear();

			var page = view.Page;

			foreach (var child in page.Children) {
				var path = child.Path;
				var button = view.Links.Button(child.Title, () => m_openPage(path), WidgetPrefabs.PageButton);

				if (button.TryGetComponent(out LayoutItem layoutItem)) {
					layoutItem.FlexibleWidth = 1;
					layoutItem.MinWidth = 80;
				}
			}

			var isEmpty = page.Children.Count == 0 && page.Segments.Count == 0;

			if (isEmpty) {
				view.Links.Label(page.IsRoot ? "No pages registered yet" : "This page is empty");
			}

			view.Links.gameObject.SetActive(page.Children.Count > 0 || isEmpty);
		}

		private void BuildSegment(PageView view, PageSegment segment)
		{
			if (view.IndexOf(segment) >= 0) {
				return;
			}

			// Builds the segment's nodes the first time anything needs them
			var root = segment.Root;

			if (root == null || !root.ShowsIn(m_context.IsOverlay)) {
				return;
			}

			// Keep the same order as the page's segments
			var index = 0;

			foreach (var other in view.Page.Segments) {
				if (other == segment) {
					break;
				}

				if (view.IndexOf(other) >= 0) {
					index++;
				}
			}

			var element = m_renderers.Render(root, view.Root, m_context);
			var previous = index > 0 ? view.Segments[index - 1].Element.Widget.transform : view.Links.transform;
			element.Widget.transform.SetSiblingIndex(previous.GetSiblingIndex() + 1);

			var mirror = NodeMirror.Create(root, element, m_context);
			view.Segments.Insert(index, new SegmentView(segment, element, mirror));
		}

		private void DisposeSegment(PageView view, int index)
		{
			var segmentView = view.Segments[index];
			view.Segments.RemoveAt(index);

			segmentView.Mirror.Dispose(true);
		}

		private void DisposeView(PageView view)
		{
			for (var i = view.Segments.Count - 1; i >= 0; i--) {
				DisposeSegment(view, i);
			}

			ContainerWidget.DestroyWidget(view.Root.gameObject);
		}

		private void OnSegmentAdded(PageSegment segment)
		{
			if (m_views.TryGetValue(segment.Node, out var view)) {
				BuildSegment(view, segment);
				view.LinksDirty = true;
			}
		}

		private void OnSegmentRemoved(PageSegment segment, PageNode page)
		{
			if (!m_views.TryGetValue(page, out var view)) {
				return;
			}

			var index = view.IndexOf(segment);

			if (index >= 0) {
				DisposeSegment(view, index);
			}

			view.LinksDirty = true;
		}

		private void OnPageRemoved(PageNode page)
		{
			if (!m_views.TryGetValue(page, out var view)) {
				return;
			}

			var wasCurrent = view == m_current;

			DisposeView(view);
			m_views.Remove(page);

			if (wasCurrent) {
				m_current = null;
				m_currentPageRemoved(page.Path);
			}
		}

		private void OnChildrenChanged(PageNode page)
		{
			// Rebuilt in Update for the current page, or when the page is next shown
			if (m_views.TryGetValue(page, out var view)) {
				view.LinksDirty = true;
			}
		}

		private class PageView
		{
			public readonly PageNode Page;
			public readonly ContainerWidget Root;
			public readonly ContainerWidget Links;
			public readonly List<SegmentView> Segments = new();

			public bool LinksDirty;
			public ScrollViewWidget.ScrollState Scroll;

			public PageView(PageNode page, ContainerWidget root, ContainerWidget links)
			{
				Page = page;
				Root = root;
				Links = links;
			}

			public int IndexOf(PageSegment segment)
			{
				for (var i = 0; i < Segments.Count; i++) {
					if (Segments[i].Segment == segment) {
						return i;
					}
				}

				return -1;
			}

			public void Tick()
			{
				foreach (var segment in Segments) {
					segment.Mirror.Tick();
				}
			}
		}

		private class SegmentView
		{
			public readonly PageSegment Segment;
			public readonly WidgetElement Element;
			public readonly NodeMirror Mirror;

			public SegmentView(PageSegment segment, WidgetElement element, NodeMirror mirror)
			{
				Segment = segment;
				Element = element;
				Mirror = mirror;
			}
		}
	}
}
