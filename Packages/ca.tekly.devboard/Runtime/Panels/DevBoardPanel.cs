using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Pages;
using Tekly.Leaf.Elements;
using Tekly.Trellis;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Tekly.DevBoard.Panels
{
	/// <summary>
	/// A docked window onto the page tree. It has a header (breadcrumb, collapse, and a "..." row of panel actions) and a scrolling
	/// body showing one page at a time. Each panel builds its own copy of the pages it visits, keeps them while
	/// they exist, and updates them as segments come and go.
	///
	/// Panels are instances of the "panel" widget prefab. The prefab provides the frame, header, actions row and
	/// body; the panel fills the body with pages and keeps the header in sync.
	///
	/// Interactive panels are for navigating. Overlay panels are pinned to one page: the body lets touches through
	/// to the game, refreshes less often, and the header shrinks to a handle that switches back to interactive.
	///
	/// When the page a panel shows is removed, an interactive panel moves up to the nearest page that still
	/// exists and an overlay hides. Either way the panel remembers the page and goes back to it if it's
	/// registered again, e.g. the next time that part of the game loads.
	/// </summary>
	public class DevBoardPanel : Widget, ITickHost
	{
		public const float OVERLAY_TICK_RATE = 10f;

		private const float OVERLAY_ALPHA = 0.85f;
		private const float MAX_HEIGHT_FRACTION = 0.85f;
		private const string ERROR_COLOR = "#E5534B";

		public string Id { get; private set; }
		public DockSlot Dock { get; private set; }
		public bool IsOverlay { get; private set; }
		public bool IsCollapsed { get; private set; }

		/// <summary>
		/// The page being shown, or null while a hidden overlay waits for its page.
		/// </summary>
		public PageNode CurrentPage => m_current?.Page;

		/// <summary>
		/// The page this panel wants to show. Differs from CurrentPage while that page doesn't exist.
		/// </summary>
		public string Path => m_waitingPath ?? m_current?.Page.Path ?? string.Empty;

		TickGroup ITickHost.TickGroup => m_tickGroup;

		private readonly TickGroup m_tickGroup = new();
		private readonly Dictionary<PageNode, PageView> m_views = new();
		private readonly HashSet<PageSegment> m_rebuildRequests = new();
		private readonly List<Action> m_deferred = new();

		private DevBoard m_board;
		private PageTree m_tree;
		private DevBoardDock m_dock;

		[Header("Header")]
		[SerializeField] private GameObject m_header;
		[SerializeField] private BreadcrumbWidget m_breadcrumb;
		[SerializeField] private ButtonWidget m_collapseButton;
		[SerializeField] private string m_collapseLabel = "-";
		[SerializeField] private string m_expandLabel = "+";

		[Tooltip("Switches the header between its normal and settings contents. Keep it outside both so it stays visible")]
		[SerializeField] private LeafToggle m_actionsToggle;

		[Tooltip("The header's normal contents, e.g. the breadcrumb. Shown while settings are closed")]
		[SerializeField] private GameObject m_headerNormal;

		[Tooltip("Shown instead of the header in overlay mode, labelled with the page title. Tapping it makes the panel interactive again")]
		[SerializeField] private ButtonWidget m_overlayHandle;

		[Header("Settings")]
		[Tooltip("The header's settings contents (panel actions). Shown instead of the normal header while the toggle is on. Any of the buttons can be left out")]
		[FormerlySerializedAs("m_actions")]
		[SerializeField] private GameObject m_headerSettings;
		[SerializeField] private ButtonWidget m_popButton;
		[SerializeField] private ButtonWidget m_dockButton;
		[SerializeField] private ButtonWidget m_overlayButton;
		[SerializeField] private ButtonWidget m_closeButton;

		[Header("Body")]
		[SerializeField] private ScrollViewWidget m_body;

		[Tooltip("Lets touches through to the game in overlay mode. Added to the body if not set")]
		[SerializeField] private CanvasGroup m_bodyGroup;

		[Header("Frame")]
		[Tooltip("Gets a MaxHeight so panels in a slot share the screen height. Found on this GameObject if not set")]
		[SerializeField] private LayoutItem m_layoutItem;

		[Tooltip("Stops blocking touches in overlay mode. Found on this GameObject if not set")]
		[SerializeField] private Graphic m_frameGraphic;

		private PageView m_current;
		private string m_waitingPath;

		private float m_tickRate;
		private float m_tickInterval;
		private float m_nextTickTime;
		private float m_maxHeight = -1f;
		private bool m_headerDirty;
		private bool m_tornDown;

		/// <summary>
		/// How many times per second widgets refresh while interactive. Zero or less ticks every frame. Overlays
		/// refresh at this rate or OVERLAY_TICK_RATE, whichever is slower.
		/// </summary>
		public DevBoardPanel WithTickRate(float ticksPerSecond)
		{
			m_tickRate = ticksPerSecond;
			UpdateTickInterval();
			return this;
		}

		internal static DevBoardPanel Create(DevBoard board, DevBoardDock dock, DevBoardPanel prefab, PanelRecord record)
		{
			var panel = Instantiate(prefab, dock.GetSlot(record.Dock), false);
			panel.name = $"Panel {record.Id}";
			panel.Setup(board, dock, record);

			return panel;
		}

		/// <summary>
		/// Shows the page at path. If it doesn't exist yet, shows the nearest page above it (or, for an overlay,
		/// hides) and switches to it once it's registered.
		/// </summary>
		public void Open(string path)
		{
			ShowPath(path);
			m_board.SavePanels();
		}

		/// <summary>
		/// Goes to the parent of the current page.
		/// </summary>
		public void Back()
		{
			var parent = m_current?.Page.Parent;

			if (parent != null) {
				Open(parent.Path);
			}
		}

		public void SetOverlay(bool overlay)
		{
			if (overlay == IsOverlay) {
				return;
			}

			ApplyOverlay(overlay);

			// Builders can lay out differently in overlays, so rebuild everything for the new mode
			ResetViews();
			m_board.SavePanels();
		}

		public void SetCollapsed(bool collapsed)
		{
			if (collapsed == IsCollapsed) {
				return;
			}

			ApplyCollapsed(collapsed);
			m_board.SavePanels();
		}

		public void SetDock(DockSlot dock)
		{
			Dock = dock;
			transform.SetParent(m_dock.GetSlot(dock), false);
			transform.SetAsLastSibling();
			m_board.SavePanels();
		}

		/// <summary>
		/// Opens the current page in a new overlay panel.
		/// </summary>
		public DevBoardPanel PopOut()
		{
			return m_board.PopOut(this);
		}

		/// <summary>
		/// Removes the panel. It isn't restored next session.
		/// </summary>
		public void Close()
		{
			m_board.ClosePanel(this);
			Teardown();
			ContainerWidget.DestroyWidget(gameObject);
		}

		internal PanelRecord ToRecord()
		{
			return new PanelRecord {
				Id = Id,
				Dock = Dock,
				Path = Path,
				Overlay = IsOverlay,
				Collapsed = IsCollapsed
			};
		}

		internal void RequestRebuild(PageSegment segment)
		{
			m_rebuildRequests.Add(segment);
		}

		private void Awake()
		{
			if (m_layoutItem == null) {
				m_layoutItem = GetComponent<LayoutItem>();
			}

			if (m_frameGraphic == null) {
				m_frameGraphic = GetComponent<Graphic>();
			}

			if (m_bodyGroup == null && m_body != null && !m_body.TryGetComponent(out m_bodyGroup)) {
				m_bodyGroup = m_body.gameObject.AddComponent<CanvasGroup>();
			}

			Wire(m_collapseButton, Deferred(() => SetCollapsed(!IsCollapsed)));
			Wire(m_overlayHandle, Deferred(() => SetOverlay(false)));
			Wire(m_popButton, MenuAction(() => PopOut()));
			Wire(m_dockButton, MenuAction(CycleDock));
			Wire(m_overlayButton, MenuAction(() => SetOverlay(true)));
			Wire(m_closeButton, MenuAction(Close));

			if (m_actionsToggle != null) {
				m_actionsToggle.onValueChanged.AddListener(OnActionsToggleChanged);
			}

			SetSettingsOpen(false);
		}

		private void Setup(DevBoard board, DevBoardDock dock, PanelRecord record)
		{
			m_board = board;
			m_tree = board.Pages;
			m_dock = dock;

			Id = record.Id;
			Dock = record.Dock;

			// The panel remembers scroll positions per page itself
			m_body.WithoutSavedState();

			m_tree.SegmentAdded += OnSegmentAdded;
			m_tree.SegmentRemoved += OnSegmentRemoved;
			m_tree.PageAdded += OnPageAdded;
			m_tree.PageRemoved += OnPageRemoved;
			m_tree.ChildrenChanged += OnChildrenChanged;

			ApplyOverlay(record.Overlay);
			ApplyCollapsed(record.Collapsed);
			ShowPath(record.Path);
		}

		private void Update()
		{
			RunDeferred();
			RunRebuilds();
			UpdateSizeLimits();

			if (m_headerDirty) {
				BuildHeader();
			}

			var now = Time.realtimeSinceStartup;

			// Hidden panels don't refresh their widgets
			if (m_board.IsVisible && now >= m_nextTickTime) {
				m_nextTickTime = now + m_tickInterval;
				m_tickGroup.Tick();
			}
		}

		private void OnDestroy()
		{
			// Scene teardown or leaving play mode: forget the panel but keep its saved layout
			m_board?.ForgetPanel(this);
			Teardown();
		}

		// ----- Navigation -----

		private void ShowPath(string path)
		{
			path = PageTree.NormalizePath(path);

			var page = m_tree.FindNearest(path);
			m_waitingPath = page.Path == path ? null : path;

			// An overlay is pinned to its page: rather than show some other page, it hides until the page is back
			var hidden = IsOverlay && m_waitingPath != null;
			gameObject.SetActive(!hidden);

			if (!hidden) {
				Show(page);
			}
		}

		private void Show(PageNode page)
		{
			if (m_current != null && m_current.Page == page) {
				m_headerDirty = true;
				return;
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
			m_headerDirty = true;
		}

		/// <summary>
		/// Throws away every built page and shows the current one again from scratch.
		/// </summary>
		private void ResetViews()
		{
			var path = Path;

			foreach (var view in m_views.Values) {
				DisposeView(view);
			}

			m_views.Clear();
			m_current = null;

			ShowPath(path);
		}

		// ----- Building pages -----

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
				var button = view.Links.Button(child.Title, Deferred(() => Open(path)), "button_page");
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

		private void RefreshLinks(PageView view)
		{
			if (view == m_current) {
				BuildLinks(view);
			} else {
				view.LinksDirty = true;
			}
		}

		private void BuildSegment(PageView view, PageSegment segment)
		{
			if (view.IndexOf(segment) >= 0) {
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

			var container = view.Root.Card();
			var previous = index > 0 ? view.Segments[index - 1].Container.transform : view.Links.transform;
			container.transform.SetSiblingIndex(previous.GetSiblingIndex() + 1);
			container.StateScope = segment.Title;

			if (segment.Title != null) {
				container.Label(segment.Title);
			}

			var context = new PageContext(this, segment, container);
			view.Segments.Insert(index, new SegmentView(segment, container, context));

			try {
				segment.Build(context);
			} catch (Exception exception) {
				Debug.LogException(exception);
				container.Label($"<color={ERROR_COLOR}>{exception.GetType().Name}: {exception.Message}</color>");
			}
		}

		private void DisposeSegment(PageView view, int index)
		{
			var segmentView = view.Segments[index];
			view.Segments.RemoveAt(index);

			segmentView.Context.Dispose();
			ContainerWidget.DestroyWidget(segmentView.Container.gameObject);
		}

		private void DisposeView(PageView view)
		{
			for (var i = view.Segments.Count - 1; i >= 0; i--) {
				DisposeSegment(view, i);
			}

			ContainerWidget.DestroyWidget(view.Root.gameObject);
		}

		private void RunRebuilds()
		{
			if (m_rebuildRequests.Count == 0) {
				return;
			}

			var segments = new List<PageSegment>(m_rebuildRequests);
			m_rebuildRequests.Clear();

			foreach (var segment in segments) {
				if (segment.Node == null || !m_views.TryGetValue(segment.Node, out var view)) {
					continue;
				}

				var index = view.IndexOf(segment);

				if (index >= 0) {
					DisposeSegment(view, index);
					BuildSegment(view, segment);
				}
			}
		}

		// ----- Page tree events -----

		private void OnSegmentAdded(PageSegment segment)
		{
			if (m_views.TryGetValue(segment.Node, out var view)) {
				BuildSegment(view, segment);
				RefreshLinks(view);
			}
		}

		private void OnSegmentRemoved(PageSegment segment, PageNode page)
		{
			m_rebuildRequests.Remove(segment);

			if (!m_views.TryGetValue(page, out var view)) {
				return;
			}

			var index = view.IndexOf(segment);

			if (index >= 0) {
				DisposeSegment(view, index);
			}

			RefreshLinks(view);
		}

		private void OnPageAdded(PageNode page)
		{
			if (m_waitingPath == page.Path) {
				ShowPath(page.Path);
				m_board.SavePanels();
			}
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

				// Keep wanting the page that went away, so the panel returns to it when it's registered again
				ShowPath(m_waitingPath ?? page.Path);
			}
		}

		private void OnChildrenChanged(PageNode page)
		{
			if (m_views.TryGetValue(page, out var view)) {
				RefreshLinks(view);
			}
		}

		// ----- Header and modes -----

		private void BuildHeader()
		{
			m_headerDirty = false;

			// Only text changes here. Which header objects are active is set directly by SetSettingsOpen and
			// ApplyOverlay, so nothing here fights the toggle.
			var page = m_current?.Page;

			if (m_overlayHandle != null) {
				m_overlayHandle.Label = page?.Title ?? PageTree.ROOT_TITLE;
			}

			if (m_breadcrumb != null) {
				var crumbs = new List<Crumb>();

				for (var node = page; node != null; node = node.Parent) {
					crumbs.Insert(0, new Crumb(node.Title, node.Path));
				}

				m_breadcrumb.Set(crumbs, path => m_deferred.Add(() => Open(path)));
			}

			if (m_collapseButton != null) {
				var label = IsCollapsed ? m_expandLabel : m_collapseLabel;

				if (!string.IsNullOrEmpty(label)) {
					m_collapseButton.Label = label;
				}
			}
		}

		/// <summary>
		/// The toggle is the only state for settings: it just swaps the normal and settings header objects. Runs
		/// immediately, since the toggle sits outside both and nothing is rebuilt.
		/// </summary>
		private void OnActionsToggleChanged(bool isOn)
		{
			ApplySettingsOpen(isOn);
		}

		private void SetSettingsOpen(bool open)
		{
			if (m_actionsToggle != null) {
				m_actionsToggle.SetIsOnWithoutNotify(open);
			}

			ApplySettingsOpen(open);
		}

		private void ApplySettingsOpen(bool open)
		{
			if (m_headerNormal != null) {
				m_headerNormal.SetActive(!open);
			}

			if (m_headerSettings != null) {
				m_headerSettings.SetActive(open);
			}
		}

		/// <summary>
		/// A button from the settings header: closes settings, then runs.
		/// </summary>
		private Action MenuAction(Action action)
		{
			return Deferred(() => {
				SetSettingsOpen(false);
				action();
			});
		}

		private void CycleDock()
		{
			var count = Enum.GetValues(typeof(DockSlot)).Length;
			SetDock((DockSlot) (((int) Dock + 1) % count));
		}

		private void ApplyOverlay(bool overlay)
		{
			IsOverlay = overlay;

			m_bodyGroup.blocksRaycasts = !overlay;
			m_bodyGroup.interactable = !overlay;
			m_bodyGroup.alpha = overlay ? OVERLAY_ALPHA : 1f;

			// Let touches around the handle through to the game too
			if (m_frameGraphic != null) {
				m_frameGraphic.raycastTarget = !overlay;
			}

			// In overlay mode the header gives way to a small handle, if the prefab has one
			var showHandle = overlay && m_overlayHandle != null;

			if (m_header != null) {
				m_header.SetActive(!showHandle);
			}

			if (m_overlayHandle != null) {
				m_overlayHandle.gameObject.SetActive(showHandle);
			}

			if (overlay) {
				SetSettingsOpen(false);
			}

			UpdateTickInterval();
			m_headerDirty = true;
		}

		private void UpdateTickInterval()
		{
			var rate = m_tickRate;

			if (IsOverlay && (rate <= 0f || rate > OVERLAY_TICK_RATE)) {
				rate = OVERLAY_TICK_RATE;
			}

			m_tickInterval = rate > 0f ? 1f / rate : 0f;
			m_nextTickTime = 0f;
		}

		private void ApplyCollapsed(bool collapsed)
		{
			IsCollapsed = collapsed;
			m_body.gameObject.SetActive(!collapsed);
			m_headerDirty = true;
		}

		/// <summary>
		/// Panels in a slot share its height, so a stack of panels doesn't run off the screen.
		/// </summary>
		private void UpdateSizeLimits()
		{
			var slot = transform.parent;

			if (slot == null) {
				return;
			}

			var panelsInSlot = 0;

			for (var i = 0; i < slot.childCount; i++) {
				if (slot.GetChild(i).gameObject.activeSelf) {
					panelsInSlot++;
				}
			}

			var maxHeight = m_dock.Area.rect.height * MAX_HEIGHT_FRACTION / Mathf.Max(1, panelsInSlot);

			if (!Mathf.Approximately(maxHeight, m_maxHeight)) {
				m_maxHeight = maxHeight;

				if (m_layoutItem != null) {
					m_layoutItem.MaxHeight = maxHeight;
				}
			}
		}

		// ----- Teardown and helpers -----

		/// <summary>
		/// Wraps a button action so it runs in the next Update rather than inside the click. Most panel actions
		/// rebuild or hide the very button that was clicked.
		/// </summary>
		private static void Wire(ButtonWidget button, Action action)
		{
			if (button != null) {
				button.WithAction(action);
			}
		}

		private Action Deferred(Action action)
		{
			return () => m_deferred.Add(action);
		}

		private void RunDeferred()
		{
			if (m_deferred.Count == 0) {
				return;
			}

			var actions = m_deferred.ToArray();
			m_deferred.Clear();

			foreach (var action in actions) {
				if (m_tornDown) {
					return;
				}

				try {
					action();
				} catch (Exception exception) {
					Debug.LogException(exception, this);
				}
			}
		}

		private void Teardown()
		{
			if (m_tornDown) {
				return;
			}

			m_tornDown = true;

			if (m_actionsToggle != null) {
				m_actionsToggle.onValueChanged.RemoveListener(OnActionsToggleChanged);
			}

			if (m_tree != null) {
				m_tree.SegmentAdded -= OnSegmentAdded;
				m_tree.SegmentRemoved -= OnSegmentRemoved;
				m_tree.PageAdded -= OnPageAdded;
				m_tree.PageRemoved -= OnPageRemoved;
				m_tree.ChildrenChanged -= OnChildrenChanged;
			}

			// Run the builders' OnDispose callbacks. The widgets go with the panel's GameObject.
			foreach (var view in m_views.Values) {
				foreach (var segment in view.Segments) {
					segment.Context.Dispose();
				}
			}

			m_views.Clear();
			m_current = null;
		}

		// ----- Built copies of pages -----

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
		}

		private class SegmentView
		{
			public readonly PageSegment Segment;
			public readonly ContainerWidget Container;
			public readonly PageContext Context;

			public SegmentView(PageSegment segment, ContainerWidget container, PageContext context)
			{
				Segment = segment;
				Container = container;
				Context = context;
			}
		}
	}
}
