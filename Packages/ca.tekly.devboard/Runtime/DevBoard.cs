using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Pages;
using Tekly.DevBoard.Panels;
using Tekly.Trellis;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.DevBoard
{
	public class DevBoard : Singleton<DevBoard>
	{
		/// <summary>
		/// The id DevBoardInit gives the panel it creates.
		/// </summary>
		public const string MAIN_PANEL_ID = "main";

		/// <summary>
		/// The widget variant panels are created from.
		/// </summary>
		public const string PANEL_VARIANT = "panel";

		/// <summary>
		/// The page DevBoard's own options are on. Other code can add segments to it too.
		/// </summary>
		public const string OPTIONS_PATH = "Options";

		/// <summary>
		/// DevBoard's own options, like the UI scale. Saved in PlayerPrefs.
		/// </summary>
		public DevBoardSettings Settings { get; } = DevBoardSettings.Load();

		/// <summary>
		/// Every registered page. Usually used through Page().
		/// </summary>
		public PageTree Pages { get; } = new();

		/// <summary>
		/// Open panels.
		/// </summary>
		public IReadOnlyList<DevBoardPanel> Panels => m_panels;

		/// <summary>
		/// View state (foldouts, scroll positions) that survives widgets being destroyed and rebuilt.
		/// </summary>
		public DevBoardState State { get; } = new();

		/// <summary>
		/// Ticks behaviours that aren't inside a panel.
		/// </summary>
		internal TickGroup DefaultTickGroup {
			get {
				Initialize();
				return m_ticker.TickGroup;
			}
		}

		/// <summary>
		/// Whether panels are shown. Hiding keeps every panel and its state, it just stops drawing them,
		/// taking touches and refreshing their widgets. Panels with ShowWhenHidden stay up either way.
		/// </summary>
		public bool IsVisible { get; private set; } = true;

		/// <summary>
		/// Raised with the new value when IsVisible changes.
		/// </summary>
		public event Action<bool> VisibilityChanged;

#if ENABLE_INPUT_SYSTEM
		/// <summary>
		/// The key that shows and hides DevBoard. Key.None turns the shortcut off.
		/// </summary>
		public Key ToggleKey { get; set; } = Key.F1;
#else
		/// <summary>
		/// The key that shows and hides DevBoard. KeyCode.None turns the shortcut off.
		/// </summary>
		public KeyCode ToggleKey { get; set; } = KeyCode.F1;
#endif

		private GameObject m_root;
		private DevBoardTicker m_ticker;
		private DevBoardDock m_dock;

		private readonly List<DevBoardPanel> m_panels = new();
		private bool m_panelsRestored;
		private bool m_restoringPanels;

		// Widget prefabs by name (the variant), and the first prefab of each exact type as that type's fallback
		private readonly Dictionary<string, Widget> m_widgets = new();
		private readonly Dictionary<Type, Widget> m_defaults = new();
		private readonly HashSet<string> m_warnings = new();

		public DevBoard()
		{
			Settings.Changed += ApplySettings;
			RegisterOptionsPage();
		}

		/// <summary>
		/// Creates the root object boards live under. Safe to call any number of times.
		/// </summary>
		public void Initialize()
		{
			// Unity null check, so a destroyed root is recreated
			if (m_root == null) {
				m_root = new GameObject("DevBoard");
				m_ticker = m_root.AddComponent<DevBoardTicker>();
				Object.DontDestroyOnLoad(m_root);
			}
		}

		/// <summary>
		/// Registers the widgets and fonts in assets. Safe to call more than once: adding the same assets again
		/// does nothing, and a widget with the same name as an existing one replaces it, so a game can add its
		/// own assets to restyle the built-in widgets.
		/// </summary>
		public void AddAssets(DevBoardAssets assets)
		{
			if (assets == null) {
				return;
			}

			Initialize();

			if (assets.Widgets != null) {
				foreach (var widget in assets.Widgets) {
					if (widget != null) {
						AddWidget(widget);
					}
				}
			}

			DevBoardFonts.Register(assets);
		}

		/// <summary>
		/// Gets the widget prefab for a variant. Falls back to the default prefab of type T, with a warning,
		/// if the variant doesn't exist or isn't a T.
		/// </summary>
		public T Get<T>(string variant) where T : Widget
		{
			if (variant != null && m_widgets.TryGetValue(variant, out var widget)) {
				if (widget is T typed) {
					return typed;
				}

				WarnOnce($"[DevBoard] Widget variant '{variant}' is a {widget.GetType().Name}, not a {typeof(T).Name}. Using the default {typeof(T).Name}.");
			} else {
				WarnOnce($"[DevBoard] No widget variant named '{variant}'. Using the default {typeof(T).Name}.");
			}

			if (m_defaults.TryGetValue(typeof(T), out var fallback)) {
				return (T) fallback;
			}

			throw new InvalidOperationException(
				$"[DevBoard] No {typeof(T).Name} widgets are registered. Has a DevBoardAssets been added with AddAssets?");
		}

		/// <summary>
		/// Gets the widget prefab for a variant without falling back or warning, for optional widgets.
		/// </summary>
		public bool TryGet<T>(string variant, out T prefab) where T : Widget
		{
			if (variant != null && m_widgets.TryGetValue(variant, out var widget) && widget is T typed) {
				prefab = typed;
				return true;
			}

			prefab = null;
			return false;
		}

		/// <summary>
		/// Adds a segment to the page at path, e.g. "Game/Economy". The page and any missing parents are created,
		/// and segments sharing a path appear one after another on the same page, sorted by order. Dispose the
		/// returned segment to remove it, or tie it to a GameObject with BindTo.
		///
		/// build runs once for every panel showing the page, and again when the segment is rebuilt, so it should
		/// read values through getters and undo anything it hooks up with PageContext.OnDispose.
		/// </summary>
		public PageSegment Page(string path, Action<PageContext> build, string title = null, int order = 0)
		{
			return Pages.Add(path, build, title, order);
		}

		/// <summary>
		/// Gets the panel with this id, creating it if needed. A panel saved from an earlier session keeps its
		/// saved dock, page and mode; dock and path only apply to a brand new panel.
		/// Returns null, with an error, if no panel prefab is registered.
		/// </summary>
		public DevBoardPanel Panel(string id, DockSlot dock = DockSlot.TopLeft, string path = null)
		{
			RestorePanels();

			var existing = FindPanel(id);

			if (existing != null) {
				return existing;
			}

			return CreatePanel(new PanelRecord {
				Id = id,
				Dock = dock,
				Path = path ?? string.Empty
			});
		}

		public DevBoardPanel FindPanel(string id)
		{
			foreach (var panel in m_panels) {
				if (panel != null && panel.Id == id) {
					return panel;
				}
			}

			return null;
		}

		/// <summary>
		/// Recreates the panels saved from the last session. Only does anything the first time it's called.
		/// Needs the widget assets, so call it after AddAssets.
		/// </summary>
		public void RestorePanels()
		{
			if (m_panelsRestored) {
				return;
			}

			// Without the prefab nothing can be restored. Leave the saved layout alone until it's registered.
			if (!TryGet(PANEL_VARIANT, out DevBoardPanel _)) {
				WarnOnce($"[DevBoard] No '{PANEL_VARIANT}' widget is registered, so panels can't be created. Add the panel prefab to a DevBoardAssets (Collect Assets).", true);
				return;
			}

			m_panelsRestored = true;
			m_restoringPanels = true;

			try {
				foreach (var record in PanelLayout.Load().Panels) {
					if (FindPanel(record.Id) == null) {
						CreatePanel(record);
					}
				}
			} finally {
				m_restoringPanels = false;
			}

			SavePanels();
		}

		/// <summary>
		/// Opens a copy of source: same page, mode and dock slot.
		/// </summary>
		internal DevBoardPanel PopOut(DevBoardPanel source)
		{
			return CreatePanel(new PanelRecord {
				Id = NextPanelId("panel"),
				Dock = source.Dock,
				Path = source.Path,
				Overlay = source.IsOverlay,
				ShowWhenHidden = source.ShowWhenHidden
			});
		}

		/// <summary>
		/// A panel was closed by the user: forget it, including its saved layout. Closing the last panel opens a
		/// new one at the root in the top left, so there's always a way back in.
		/// </summary>
		internal void ClosePanel(DevBoardPanel panel)
		{
			m_panels.Remove(panel);
			m_panels.RemoveAll(other => other == null);

			if (m_panels.Count == 0) {
				CreatePanel(new PanelRecord {
					Id = NextPanelId(MAIN_PANEL_ID),
					Dock = DockSlot.TopLeft,
					Path = string.Empty
				});
			}

			SavePanels();
		}

		/// <summary>
		/// Shows or hides panels to match IsVisible and their ShowWhenHidden. Called by panels when that changes.
		/// </summary>
		internal void ApplyVisibility()
		{
			// Unity null check on the canvas: the dock may not exist yet, or may have been destroyed
			if (m_dock == null || m_dock.Canvas == null) {
				return;
			}

			var anyShown = IsVisible;

			foreach (var panel in m_panels) {
				if (panel != null) {
					panel.RefreshActive();
					anyShown |= panel.ShowWhenHidden;
				}
			}

			// Nothing to draw: turn the whole canvas off rather than keep an empty one around
			m_dock.Canvas.enabled = anyShown;
		}

		private string NextPanelId(string prefix)
		{
			if (FindPanel(prefix) == null) {
				return prefix;
			}

			var number = 2;

			while (FindPanel($"{prefix}-{number}") != null) {
				number++;
			}

			return $"{prefix}-{number}";
		}

		private DockSlot FindEmptySlot(DockSlot fallback)
		{
			foreach (DockSlot slot in Enum.GetValues(typeof(DockSlot))) {
				var used = false;

				foreach (var panel in m_panels) {
					if (panel != null && panel.Dock == slot) {
						used = true;
						break;
					}
				}

				if (!used) {
					return slot;
				}
			}

			return fallback;
		}

		/// <summary>
		/// A panel was destroyed some other way (scene teardown, leaving play mode): forget it but keep its saved layout.
		/// </summary>
		internal void ForgetPanel(DevBoardPanel panel)
		{
			m_panels.Remove(panel);
		}

		internal void SavePanels()
		{
			// Saving before or during the restore would overwrite panels that haven't been recreated yet
			if (!m_panelsRestored || m_restoringPanels) {
				return;
			}

			var layout = new PanelLayout();

			foreach (var panel in m_panels) {
				if (panel != null) {
					layout.Panels.Add(panel.ToRecord());
				}
			}

			layout.Save();
		}

		public void SetVisible(bool visible)
		{
			if (IsVisible == visible) {
				return;
			}

			IsVisible = visible;
			ApplyVisibility();
			VisibilityChanged?.Invoke(visible);
		}

		public void ToggleVisible()
		{
			SetVisible(!IsVisible);
		}

		/// <summary>
		/// Called every frame by DevBoardTicker.
		/// </summary>
		internal void CheckToggleKey()
		{
#if ENABLE_INPUT_SYSTEM
			var keyboard = Keyboard.current;

			if (ToggleKey != Key.None && keyboard != null && keyboard[ToggleKey].wasPressedThisFrame) {
				ToggleVisible();
			}
#elif ENABLE_LEGACY_INPUT_MANAGER
			if (ToggleKey != KeyCode.None && Input.GetKeyDown(ToggleKey)) {
				ToggleVisible();
			}
#endif
		}

		/// <summary>
		/// Where floating widgets like an open dropdown go, above every panel. Null until a panel has been created.
		/// </summary>
		internal RectTransform PopupLayer => m_dock != null && m_dock.Canvas != null ? m_dock.Popups : null;

		private DevBoardPanel CreatePanel(PanelRecord record)
		{
			Initialize();

			// Unity null check on the canvas, so a destroyed dock is recreated along with the root
			if (m_dock == null || m_dock.Canvas == null) {
				m_dock = new DevBoardDock(m_root.transform);
				m_dock.ApplySettings(Settings);
				ApplyVisibility();
			}

			if (!TryGet(PANEL_VARIANT, out DevBoardPanel prefab)) {
				WarnOnce($"[DevBoard] No '{PANEL_VARIANT}' widget is registered, so panels can't be created. Add the panel prefab to a DevBoardAssets (Collect Assets).", true);
				return null;
			}

			var panel = DevBoardPanel.Create(this, m_dock, prefab, record);
			m_panels.Add(panel);
			ApplyVisibility();
			SavePanels();

			return panel;
		}

		private void AddWidget(Widget widget)
		{
			var type = widget.GetType();

			if (m_widgets.TryGetValue(widget.name, out var existing)) {
				if (existing == widget) {
					return;
				}

				// A replacement of the same type also takes over as that type's default
				var existingType = existing.GetType();

				if (m_defaults.TryGetValue(existingType, out var existingDefault) && existingDefault == existing) {
					if (existingType == type) {
						m_defaults[type] = widget;
					} else {
						m_defaults.Remove(existingType);
					}
				}
			}

			m_widgets[widget.name] = widget;
			m_defaults.TryAdd(type, widget);
		}

		private void ApplySettings()
		{
			// Unity null check on the canvas: the dock may not exist yet, or may have been destroyed
			if (m_dock != null && m_dock.Canvas != null) {
				m_dock.ApplySettings(Settings);
			}
		}

		private string LabelAndSubLabel(string label, string subLabel)
		{
			return $"{label}\n<style=\"sublabel\">{subLabel}</style>";
		}

		private void RegisterOptionsPage()
		{
			Page(OPTIONS_PATH, page => {
				var form = page.Root.Form();
				form.FloatInput(LabelAndSubLabel("UI Scale", "Scales all UI Elements"), () => Settings.Scale, value => Settings.Scale = value)
					.WithFormat("0.##");

				var scaleRow = form.Row().WithPadding(0).WithAlignment(LayoutAlignment.End).WithFlexibleWidth();
				scaleRow.Button("-", () => Settings.Scale -= DevBoardSettings.SCALE_STEP);
				scaleRow.Button("+", () => Settings.Scale += DevBoardSettings.SCALE_STEP);
				scaleRow.Button("1x", () => Settings.Scale = 1f);

#if UNITY_EDITOR
				form.Toggle(LabelAndSubLabel("Use Physical Size Scale", "Use screen DPI to scale UI"), () => Settings.KeepPhysicalSizeInEditor,
					value => Settings.KeepPhysicalSizeInEditor = value);
#endif

				form.Button("Reset options", Settings.ResetToDefaults);
			}, order: int.MaxValue);
		}

		private void WarnOnce(string message, bool isError = false)
		{
			if (!m_warnings.Add(message)) {
				return;
			}

			if (isError) {
				Debug.LogError(message);
			} else {
				Debug.LogWarning(message);
			}
		}
	}
}
