using System;
using System.Collections;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.Common.Ui.Fancy;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Nodes;
using Tekly.DevBoard.Pages;
using Tekly.DevBoard.Views;
using Tekly.Leaf.Elements;
using Tekly.Leaf.Elements.Radios;
using Tekly.Trellis;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Tekly.DevBoard.Panels
{
	/// <summary>
	/// A docked window onto the page tree. It has a header (breadcrumb, collapse, and a "..." row of panel actions) and a scrolling
	/// body showing one page at a time. Each panel renders its own widgets for the pages it visits from their
	/// segments' nodes, keeps them while the pages exist, and updates them as segments and nodes come and go.
	///
	/// Panels are instances of the "panel" widget prefab. The prefab provides the frame, header, actions row and
	/// body; the panel fills the body with pages and keeps the header in sync.
	///
	/// Interactive panels are for navigating. Overlay panels show their page over the game: the body lets touches
	/// through, refreshes less often, and the header shrinks to a handle that switches back to interactive. An
	/// overlay whose page goes away hides until it's back, rather than showing the page above it.
	///
	/// The work is split three ways: PanelHeader runs the header, PageHost the pages in the body, and
	/// PanelNavigation which page is wanted. This class holds the panel's state and ties them together.
	/// </summary>
	public class DevBoardPanel : Widget, ITickHost, IViewHost
	{
		public const float OVERLAY_TICK_RATE = 10f;
		private const float OVERLAY_ALPHA = 0.85f;


		private const float FLASH_TIME = 0.8f;
		private const float REVEAL_TIMEOUT = 1f;
		private static readonly Color s_flashColor = new(0.4f, 0.7f, 1f, 0.35f);

		public string Id { get; private set; }
		public DockSlot Dock { get; private set; }
		public bool IsOverlay { get; private set; }
		public bool IsCollapsed { get; private set; }

		/// <summary>
		/// Stays on screen while DevBoard is hidden, e.g. a perf readout.
		/// </summary>
		public bool ShowWhenHidden { get; private set; }

		/// <summary>
		/// The page being shown, or null while a hidden overlay waits for its page.
		/// </summary>
		public PageNode CurrentPage => m_pages?.CurrentPage;

		/// <summary>
		/// The page this panel wants to show. Differs from CurrentPage while that page doesn't exist.
		/// </summary>
		public string Path => m_navigation?.Path ?? string.Empty;

		TickGroup ITickHost.TickGroup => m_tickGroup;

		private readonly TickGroup m_tickGroup = new();
		private readonly List<Action> m_deferred = new();

		private DevBoardUI m_board;
		private DevBoardDock m_dock;
		private PanelHeader m_panelHeader;
		private PageHost m_pages;
		private PanelNavigation m_navigation;

		[Header("Header")]
		[SerializeField] private GameObject m_header;
		[SerializeField] private BreadcrumbWidget m_breadcrumb;
		[Tooltip("On while the panel is collapsed")]
		[SerializeField] private LeafToggle m_collapseToggle;

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
		[Tooltip("Picks the dock slot. Each option needs a DockSlotOption saying which slot it is")]
		[SerializeField] private LeafRadioGroup m_dockGroup;
		[Tooltip("Optional. Switches the panel between interactive and overlay")]
		[SerializeField] private LeafToggle m_overlayToggle;

		[Tooltip("On while the panel stays on screen when DevBoard is hidden")]
		[SerializeField] private LeafToggle m_showWhenHiddenToggle;

		[SerializeField] private ButtonWidget m_closeButton;

		[Header("Body")]
		[SerializeField] private ScrollViewWidget m_body;

		[Tooltip("Lets touches through to the game in overlay mode. Added to the body if not set")]
		[SerializeField] private CanvasGroup m_bodyGroup;

		[Header("Frame")]
		[Tooltip("Gets a MaxHeight so panels in a slot share the screen height. Found on this GameObject if not set")]
		[SerializeField] private LayoutItem m_layoutItem;

		[Tooltip("The least width while showing a page that only lists other pages, so lists aren't skinny next to " +
			"the pages they open. 0 for none. How fast the panel eases between page sizes is the Size Transition on " +
			"its layout")]
		[SerializeField, Min(0f)] private float m_linkPageMinWidth = 280f;

		[Tooltip("Stops blocking touches in overlay mode. Found on this GameObject if not set")]
		[SerializeField] private Graphic m_frameGraphic;

		private float m_tickRate;
		private float m_tickInterval;
		private float m_nextTickTime;
		private float m_maxHeight = -1f;
		private bool m_frameSetUp;
		private OptionalFloat m_prefabMinWidth;
		private float m_prefabSizeTransition;
		private bool m_showingLinkPage;
		private PageNode m_sizedPage;
		private int m_pageChangedFrame = -1;
		private bool m_headerDirty;
		private bool m_tornDown;
		private Coroutine m_revealRoutine;

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

		internal static DevBoardPanel Create(DevBoardUI board, DevBoardDock dock, DevBoardPanel prefab, PanelRecord record)
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
			m_navigation.ShowPath(path);
			m_board.SavePanels();
		}

		/// <summary>
		/// Shows where node is: opens its page, opens foldouts on the way, scrolls to it, selects it and flashes it.
		/// Selecting an input puts the keyboard in it, ready to type; anything else is just selected, so a dropdown
		/// stays closed until Enter. False if node isn't on a page or can't be shown in this panel, e.g. VisibleWhen
		/// is hiding it, it's only for the other mode, or it's a list row this panel's search is hiding.
		/// </summary>
		public bool Reveal(Node node)
		{
			var page = Segment.Of(node)?.Source.Node;

			if (page == null || page.IsRemoved) {
				return false;
			}

			SetCollapsed(false);
			Open(page.Path);

			var widget = m_pages.Reveal(node);

			if (widget == null) {
				return false;
			}

			if (m_revealRoutine != null) {
				StopCoroutine(m_revealRoutine);
			}

			if (isActiveAndEnabled) {
				m_revealRoutine = StartCoroutine(RevealAfterLayout(widget));
			}

			return true;
		}

		/// <summary>
		/// Goes to the parent of the current page.
		/// </summary>
		public void Back()
		{
			var parent = CurrentPage?.Parent;

			if (parent != null) {
				Open(parent.Path);
			}
		}

		public void SetOverlay(bool overlay)
		{
			if (overlay == IsOverlay) {
				return;
			}

			var path = Path;
			ApplyOverlay(overlay);

			// Nodes can be limited to one mode, so render everything again for the new mode
			m_pages.Reset();
			m_navigation.ShowPath(path);

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

		public void SetShowWhenHidden(bool showWhenHidden)
		{
			if (showWhenHidden == ShowWhenHidden) {
				return;
			}

			ShowWhenHidden = showWhenHidden;
			m_panelHeader.ShowShowWhenHidden(showWhenHidden);
			m_board.ApplyVisibility();
			m_board.SavePanels();
		}

		public void SetDock(DockSlot dock)
		{
			if (dock == Dock) {
				m_panelHeader.ShowDock();
				return;
			}

			Dock = dock;
			ApplyDockCorners();
			transform.SetParent(m_dock.GetSlot(dock), false);
			transform.SetAsLastSibling();
			m_panelHeader.ShowDock();
			m_board.SavePanels();
		}

		/// <summary>
		/// Squares the corners that touch a screen edge, so a docked panel sits flush against it.
		/// </summary>
		private void ApplyDockCorners()
		{
			if (!TryGetComponent(out FancyRect rect)) {
				return;
			}

			var square = Dock switch {
				DockSlot.TopLeft => RectCorners.TopLeft | RectCorners.TopRight | RectCorners.BottomLeft,
				DockSlot.Top => RectCorners.Top,
				DockSlot.TopRight => RectCorners.TopLeft | RectCorners.TopRight | RectCorners.BottomRight,
				DockSlot.BottomRight => RectCorners.BottomLeft | RectCorners.BottomRight | RectCorners.TopRight,
				DockSlot.Bottom => RectCorners.Bottom,
				_ => RectCorners.BottomLeft | RectCorners.BottomRight | RectCorners.TopLeft
			};

			rect.RoundedCorners = RectCorners.All & ~square;
		}

		/// <summary>
		/// Opens a copy of this panel in another dock slot.
		/// </summary>
		public DevBoardPanel PopOut()
		{
			return m_board.PopOut(this);
		}

		/// <summary>
		/// Removes the panel. It isn't restored next session. Closing the last panel opens a new one at the root.
		/// </summary>
		public void Close()
		{
			m_board.ClosePanel(this);
			Teardown();
			ContainerWidget.DestroyWidget(gameObject);
		}

		/// <summary>
		/// Links inside pages call this from their click, so the page changes after the click has finished.
		/// </summary>
		void IViewHost.Open(string path)
		{
			Defer(() => Open(path));
		}

		/// <summary>
		/// Runs action in the next Update rather than straight away. Most panel actions rebuild or hide the very
		/// button that was clicked to start them.
		/// </summary>
		internal void Defer(Action action)
		{
			m_deferred.Add(action);
		}

		internal PanelRecord ToRecord()
		{
			return new PanelRecord {
				Id = Id,
				Dock = Dock,
				Path = Path,
				Overlay = IsOverlay,
				Collapsed = IsCollapsed,
				ShowWhenHidden = ShowWhenHidden
			};
		}

		/// <summary>
		/// Active while it has a page to show and either DevBoard is visible or the panel shows when hidden.
		/// Inactive panels keep their built pages but don't draw, take touches or tick.
		/// </summary>
		internal void RefreshActive()
		{
			var waitingHidden = m_navigation != null && m_navigation.IsWaitingHidden;
			var active = !waitingHidden && (m_board.IsVisible || ShowWhenHidden);

			if (gameObject.activeSelf != active) {
				gameObject.SetActive(active);
			}
		}

		private void Awake()
		{
			FindParts();
		}

		/// <summary>
		/// Fills in the optional parts and sets up the header. Runs from Awake or Setup, whichever is first: Awake
		/// waits when the panel is created under an inactive parent.
		/// </summary>
		private void FindParts()
		{
			if (m_panelHeader != null) {
				return;
			}

			if (m_layoutItem == null) {
				m_layoutItem = GetComponent<LayoutItem>();
			}

			if (m_frameGraphic == null) {
				m_frameGraphic = GetComponent<Graphic>();
			}

			if (m_bodyGroup == null && m_body != null && !m_body.TryGetComponent(out m_bodyGroup)) {
				m_bodyGroup = m_body.gameObject.AddComponent<CanvasGroup>();
			}

			m_panelHeader = new PanelHeader(this, new PanelHeader.Parts {
				Header = m_header,
				Breadcrumb = m_breadcrumb,
				CollapseToggle = m_collapseToggle,
				ActionsToggle = m_actionsToggle,
				HeaderNormal = m_headerNormal,
				OverlayHandle = m_overlayHandle,
				HeaderSettings = m_headerSettings,
				PopButton = m_popButton,
				DockGroup = m_dockGroup,
				OverlayToggle = m_overlayToggle,
				ShowWhenHiddenToggle = m_showWhenHiddenToggle,
				CloseButton = m_closeButton
			});
		}

		private void Setup(DevBoardUI board, DevBoardDock dock, PanelRecord record)
		{
			FindParts();

			m_board = board;
			m_dock = dock;

			Id = record.Id;
			Dock = record.Dock;
			ApplyDockCorners();
			ShowWhenHidden = record.ShowWhenHidden;
			m_panelHeader.ShowShowWhenHidden(ShowWhenHidden);

			m_pages = new PageHost(DevBoard.Instance.Pages, m_body, board.Renderers, new ViewContext(this),
				path => Defer(() => Open(path)),
				path => m_navigation.CurrentPageRemoved(path));

			m_navigation = new PanelNavigation(DevBoard.Instance.Pages, m_pages, () => IsOverlay, OnShownChanged, m_board.SavePanels);

			ApplyOverlay(record.Overlay);
			ApplyCollapsed(record.Collapsed);
			m_navigation.ShowPath(record.Path);
		}

		private void Update()
		{
			RunDeferred();

			m_navigation.Update();
			m_pages.Update();
			UpdateSizeLimits();
			UpdateFrameSize();
			UpdateScrollbarFreeze();

			if (m_headerDirty) {
				m_headerDirty = false;
				m_panelHeader.ShowPage(CurrentPage);
			}

			var now = Time.realtimeSinceStartup;

			// Hidden panels are inactive, so this only runs while the panel is on screen
			if (now >= m_nextTickTime) {
				m_nextTickTime = now + m_tickInterval;
				m_tickGroup.Tick();
				m_pages.Tick();
			}
		}

		private void OnDestroy()
		{
			// Scene teardown or leaving play mode: forget the panel but keep its saved layout
			m_board?.ForgetPanel(this);
			Teardown();
		}

		private IEnumerator RevealAfterLayout(Widget widget)
		{
			var target = (RectTransform) widget.transform;
			var content = m_body.Content;
			var deadline = Time.realtimeSinceStartup + REVEAL_TIMEOUT;

			var lastPosition = new Vector2(float.NaN, float.NaN);
			var lastSize = Vector2.zero;
			var lastContentSize = Vector2.zero;

			// A page or panel shown this frame can take a few frames to lay out: text measures itself, fitted
			// containers resize, and the body's content grows. Scrolling before then aims at the wrong place, or is
			// clamped back by content that isn't tall enough yet. So wait until the target stops moving and the
			// content stops resizing for a frame.
			while (true) {
				yield return null;

				if (widget == null) {
					m_revealRoutine = null;
					yield break;
				}

				// Relative to the content, so scrolling doesn't count as moving
				var position = (Vector2) content.InverseTransformPoint(target.position);
				var size = target.rect.size;
				var contentSize = content.rect.size;

				var settled = position == lastPosition && size == lastSize && contentSize == lastContentSize;

				if (settled || Time.realtimeSinceStartup > deadline) {
					break;
				}

				lastPosition = position;
				lastSize = size;
				lastContentSize = contentSize;
			}

			m_revealRoutine = null;

			if (!widget.isActiveAndEnabled) {
				yield break;
			}

			// Innermost first, e.g. a list's own scroll view, then the panel's body
			for (var parent = target.parent; parent != null; parent = parent.parent) {
				if (parent.TryGetComponent(out ScrollViewWidget scroll)) {
					scroll.ScrollIntoView(target);
				}

				if (parent == transform) {
					break;
				}
			}

			Select(widget);
			StartCoroutine(Flash(target));
		}

		/// <summary>
		/// Selects the widget's first selectable, so the keyboard acts on it next. Input fields start editing when
		/// selected, with their text selected so typing replaces it.
		/// </summary>
		private static void Select(Widget widget)
		{
			var eventSystem = EventSystem.current;
			var selectable = widget.GetComponentInChildren<Selectable>();

			if (eventSystem == null || selectable == null) {
				return;
			}

			if (selectable is TMP_InputField input) {
				input.onFocusSelectAll = true;
			}

			eventSystem.SetSelectedGameObject(selectable.gameObject);
		}

		/// <summary>
		/// A tint over target that fades out, to show where it is.
		/// </summary>
		private static IEnumerator Flash(RectTransform target)
		{
			var flashObject = new GameObject("Reveal Flash", typeof(RectTransform));
			var rect = (RectTransform) flashObject.transform;
			rect.SetParent(target, false);
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;

			// On top of the widget, but not part of its layout and not catching touches
			flashObject.AddComponent<LayoutItem>().IgnoreLayout = true;
			var image = flashObject.AddComponent<Image>();
			image.raycastTarget = false;

			for (var time = 0f; time < FLASH_TIME; time += Time.unscaledDeltaTime) {
				// Gone with the widget
				if (image == null) {
					yield break;
				}

				var color = s_flashColor;
				color.a *= 1f - time / FLASH_TIME;
				image.color = color;

				yield return null;
			}

			if (flashObject != null) {
				Destroy(flashObject);
			}
		}

		private void OnShownChanged()
		{
			RefreshActive();
			m_headerDirty = true;
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

			m_panelHeader.ApplyOverlay(overlay);

			UpdateTickInterval();
			m_headerDirty = true;
		}

		private void ApplyCollapsed(bool collapsed)
		{
			IsCollapsed = collapsed;
			m_body.gameObject.SetActive(!collapsed);
			m_panelHeader.ShowCollapsed(collapsed);
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

		/// <summary>
		/// Panels in a slot share the Max Height from the options, so a stack of panels doesn't run off the screen.
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

			var fraction = m_board != null ? m_board.Settings.MaxPanelHeight : DevBoardSettings.DEFAULT_PANEL_HEIGHT;
			var maxHeight = m_dock.Area.rect.height * fraction / Mathf.Max(1, panelsInSlot);

			if (!Mathf.Approximately(maxHeight, m_maxHeight)) {
				m_maxHeight = maxHeight;

				if (m_layoutItem != null) {
					m_layoutItem.MaxHeight = maxHeight;
				}
			}
		}

		/// <summary>
		/// Widens pages that only list other pages, and eases between page sizes with the Size Transition on the
		/// panel's layout, unless animations are turned off in the options.
		/// </summary>
		private void UpdateFrameSize()
		{
			var layout = m_layoutItem != null ? m_layoutItem : GetComponent<LayoutItem>();

			if (layout == null) {
				return;
			}

			if (!m_frameSetUp) {
				m_frameSetUp = true;
				m_prefabMinWidth = layout.MinWidth;
				m_prefabSizeTransition = layout.SizeTransition;
			}

			var sizeTransition = m_board == null || m_board.Settings.Animations ? m_prefabSizeTransition : 0f;

			if (!Mathf.Approximately(layout.SizeTransition, sizeTransition)) {
				layout.SizeTransition = sizeTransition;
			}

			var page = CurrentPage;
			var isLinkPage = page != null && page.Segments.Count == 0 && page.Children.Count > 0;

			if (isLinkPage == m_showingLinkPage) {
				return;
			}

			m_showingLinkPage = isLinkPage;

			// The prefab's own min width is kept, and put back on other pages
			layout.MinWidth = isLinkPage
				? new OptionalFloat(Mathf.Max(m_linkPageMinWidth, m_prefabMinWidth.GetValueOrDefault()))
				: m_prefabMinWidth;
		}

		/// <summary>
		/// Keeps the body's scrollbars from flashing while the panel eases to a new size, when the content is
		/// briefly bigger than the panel. Frozen as soon as the page changes, before the new page is laid out, and
		/// whenever a size transition runs, then let go once the panel has settled.
		/// </summary>
		private void UpdateScrollbarFreeze()
		{
			if (m_body == null) {
				return;
			}

			var page = CurrentPage;

			if (page != m_sizedPage) {
				m_sizedPage = page;
				m_pageChangedFrame = Time.frameCount;
			}

			var layout = m_layoutItem != null ? m_layoutItem : GetComponent<LayoutItem>();
			var transitioning = layout != null && layout.SizeTransition > 0f && layout.IsSizeTransitioning;

			// The page's layout, which starts the transition, runs after this frame's Update
			var pageJustChanged = Time.frameCount <= m_pageChangedFrame + 1;

			m_body.FreezeScrollbars(transitioning || (pageJustChanged && layout != null && layout.SizeTransition > 0f));
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

			m_panelHeader?.Teardown();
			m_navigation?.Teardown();
			m_pages?.Teardown();
		}
	}
}
