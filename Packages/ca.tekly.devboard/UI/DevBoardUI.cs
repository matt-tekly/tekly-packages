using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Console;
using Tekly.DevBoard.Nodes;
using Tekly.DevBoard.Pages;
using Tekly.DevBoard.Palette;
using Tekly.DevBoard.Panels;
using Tekly.DevBoard.Views;
using Tekly.Trellis;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace Tekly.DevBoard
{
	/// <summary>
	/// DevBoard's panels: the uGUI side, only in builds that include DevBoard (see DevBoard.Enabled). It shows the
	/// pages registered with DevBoard.Page in docked panels, and runs the command palette and the keyboard
	/// shortcuts. Game code rarely needs it; DevBoardInit sets it up.
	/// </summary>
	public class DevBoardUI : Singleton<DevBoardUI>
	{
		/// <summary>
		/// The id DevBoardInit gives the panel it creates.
		/// </summary>
		public const string MAIN_PANEL_ID = "main";

		/// <summary>
		/// The page DevBoard's own options are on. Other code can add segments to it too.
		/// </summary>
		public const string OPTIONS_PATH = "System/Options";

		/// <summary>
		/// DevBoard's own options, like the UI scale. Saved in PlayerPrefs.
		/// </summary>
		public DevBoardSettings Settings { get; } = DevBoardSettings.Load();

		/// <summary>
		/// Open panels.
		/// </summary>
		public IReadOnlyList<DevBoardPanel> Panels => m_panels;

		/// <summary>
		/// How each kind of node becomes a widget. Register here to restyle a built-in node or add your own.
		/// </summary>
		public NodeRenderers Renderers { get; } = NodeRenderers.CreateDefault();

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

		/// <summary>
		/// The key that opens and closes the console. Key.None turns the shortcut off. The palette opens with
		/// Ctrl+F while DevBoard has focus.
		/// </summary>
		public Key ConsoleKey { get; set; } = Key.F2;
#else
		/// <summary>
		/// The key that shows and hides DevBoard. KeyCode.None turns the shortcut off.
		/// </summary>
		public KeyCode ToggleKey { get; set; } = KeyCode.F1;

		/// <summary>
		/// The key that opens and closes the console. KeyCode.None turns the shortcut off. The palette opens with
		/// Ctrl+F while DevBoard has focus.
		/// </summary>
		public KeyCode ConsoleKey { get; set; } = KeyCode.F2;
#endif

		private GameObject m_root;
		private DevBoardTicker m_ticker;
		private DevBoardDock m_dock;

		private readonly List<DevBoardPanel> m_panels = new();
		private DevBoardPanel m_lastUsedPanel;
		private bool m_lastPressOnBoard;
		private readonly List<RaycastResult> m_pressHits = new();
		private bool m_panelsRestored;
		private bool m_restoringPanels;

		// Widget prefabs by name, and the first prefab of each exact type as that type's fallback
		private readonly Dictionary<string, Widget> m_widgets = new();
		private readonly Dictionary<Type, Widget> m_defaults = new();
		private readonly HashSet<string> m_warnings = new();

		private readonly CommandPalette m_palette;
		private readonly DevConsole m_console;

		public DevBoardUI()
		{
			m_palette = new CommandPalette(this);
			m_console = new DevConsole(this);

			Settings.Changed += ApplySettings;
			RegisterOptionsPage();
		}

		/// <summary>
		/// Opens the command palette: search every page and control by name.
		/// </summary>
		public void OpenPalette()
		{
			Initialize();
			m_palette.Open();
		}

		public void ClosePalette()
		{
			m_palette.Close();
		}

		/// <summary>
		/// Opens the console: a terminal for typing commands, along the bottom of the screen.
		/// </summary>
		public void OpenConsole()
		{
			Initialize();
			m_console.Open();
		}

		public void CloseConsole()
		{
			m_console.Close();
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
		/// Gets the registered prefab. Falls back to the default prefab of type T, with a warning, if there's
		/// no prefab registered under that name or it isn't a T.
		/// </summary>
		public T Get<T>(WidgetPrefab<T> prefab) where T : Widget
		{
			if (prefab.IsSet && m_widgets.TryGetValue(prefab.Name, out var widget)) {
				if (widget is T typed) {
					return typed;
				}

				WarnOnce($"[DevBoard] Widget prefab '{prefab}' is a {widget.GetType().Name}, not a {typeof(T).Name}. Using the default {typeof(T).Name}.");
			} else {
				WarnOnce($"[DevBoard] No widget prefab named '{prefab}'. Using the default {typeof(T).Name}.");
			}

			if (m_defaults.TryGetValue(typeof(T), out var fallback)) {
				return (T) fallback;
			}

			throw new InvalidOperationException(
				$"[DevBoard] No {typeof(T).Name} widgets are registered. Has a DevBoardAssets been added with AddAssets?");
		}

		/// <summary>
		/// Gets the registered prefab without falling back or warning, for optional widgets.
		/// </summary>
		public bool TryGet<T>(WidgetPrefab<T> prefab, out T asset) where T : Widget
		{
			if (prefab.IsSet && m_widgets.TryGetValue(prefab.Name, out var widget) && widget is T typed) {
				asset = typed;
				return true;
			}

			asset = null;
			return false;
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
			if (!TryGet(WidgetPrefabs.Panel, out DevBoardPanel _)) {
				WarnOnce($"[DevBoard] No '{WidgetPrefabs.Panel}' widget is registered, so panels can't be created. Add the panel prefab to a DevBoardAssets (Collect Assets).", true);
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
				CreateRootPanel();
			}

			SavePanels();
		}

		/// <summary>
		/// A new interactive panel at the root in the top left, with an id no other panel has.
		/// </summary>
		internal DevBoardPanel CreateRootPanel()
		{
			return CreatePanel(new PanelRecord {
				Id = NextPanelId(MAIN_PANEL_ID),
				Dock = DockSlot.TopLeft,
				Path = string.Empty
			});
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

			// The palette floats on the panels' canvas, and can be open while the panels are hidden
			var anyShown = IsVisible || m_palette.IsOpen || m_console.IsOpen;

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
		/// Called every frame by DevBoardTicker: the shortcuts, and the palette's and console's keys.
		/// </summary>
		internal void UpdateKeys()
		{
			if (WasPressed(ToggleKey)) {
				ToggleVisible();
			}

			// The same key closes it again
			if (WasPressed(ConsoleKey)) {
				if (m_console.IsOpen) {
					CloseConsole();
				} else {
					OpenConsole();
				}
			}

			// Ctrl+F only while DevBoard has focus, so the game can use it otherwise
			if (KeyInput.Find && IsFocused) {
				OpenPalette();
			}

			NoteUsedPanel();
			DevBoard.Instance.SetFocused(IsFocused);
			m_palette.Update();
			m_console.Update();
		}

		/// <summary>
		/// True while DevBoard has the user's attention: the last click or touch landed on its UI, or something in it
		/// is selected, like an input being typed in.
		/// </summary>
		public bool IsFocused {
			get {
				if (m_dock == null || m_dock.Canvas == null || !m_dock.Canvas.enabled) {
					return false;
				}

				var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
				return m_lastPressOnBoard || IsOnBoard(selected);
			}
		}

		private bool IsOnBoard(GameObject gameObject)
		{
			return gameObject != null && m_dock != null && m_dock.Canvas != null && gameObject.transform.IsChildOf(m_dock.Canvas.transform);
		}

		/// <summary>
		/// The panel clicked or touched most recently, while it's still showing. Null if there isn't one.
		/// </summary>
		public DevBoardPanel LastUsedPanel =>
			m_lastUsedPanel != null && m_lastUsedPanel.isActiveAndEnabled ? m_lastUsedPanel : null;

		/// <summary>
		/// Remembers which panel a click or touch began on: the panel above whatever the UI hit there.
		/// Polled rather than heard through UI events, since the buttons and fields inside a panel take those first.
		/// A press on a popup, like the palette, hits the popup and leaves the panel as it was. Also notes whether the
		/// press was on DevBoard at all, for IsFocused.
		/// </summary>
		private void NoteUsedPanel()
		{
			var eventSystem = EventSystem.current;

			if (eventSystem == null || !KeyInput.TryGetPress(out var position)) {
				return;
			}

			// Made per press, which is rare enough not to matter, so it never belongs to an old event system
			var pressData = new PointerEventData(eventSystem) { position = position };

			m_pressHits.Clear();
			eventSystem.RaycastAll(pressData, m_pressHits);

			m_lastPressOnBoard = m_pressHits.Count > 0 && IsOnBoard(m_pressHits[0].gameObject);

			if (m_pressHits.Count == 0) {
				return;
			}

			var panel = m_pressHits[0].gameObject.GetComponentInParent<DevBoardPanel>();

			if (panel != null) {
				m_lastUsedPanel = panel;
			}
		}

#if ENABLE_INPUT_SYSTEM
		private static bool WasPressed(Key key)
		{
			var keyboard = Keyboard.current;
			return key != Key.None && keyboard != null && keyboard[key].wasPressedThisFrame;
		}
#else
		private static bool WasPressed(KeyCode key)
		{
#if ENABLE_LEGACY_INPUT_MANAGER
			return key != KeyCode.None && Input.GetKeyDown(key);
#else
			return false;
#endif
		}
#endif

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

			if (!TryGet(WidgetPrefabs.Panel, out DevBoardPanel prefab)) {
				WarnOnce($"[DevBoard] No '{WidgetPrefabs.Panel}' widget is registered, so panels can't be created. Add the panel prefab to a DevBoardAssets (Collect Assets).", true);
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

		/// <summary>
		/// DevBoard's own settings. Sorted last among the top pages, as its order is int.MaxValue.
		/// </summary>
		private void RegisterOptionsPage()
		{
			DevBoard.Instance.AddPage(OPTIONS_PATH, page => {
				var form = page.Form();

				// The steps sit right after the input rather than on a row of their own
				var scaleRow = form.Row().WithPadding(0).WithCrossAlignment(CrossAlignment.Center);
				scaleRow.FloatInput("UI Scale", () => Settings.Scale, value => Settings.Scale = value)
					.WithFormat("0.##")
					.WithDescription($"Scales all of DevBoard, from {DevBoardSettings.MIN_SCALE}x to {DevBoardSettings.MAX_SCALE}x");
				scaleRow.Button("-", () => Settings.Scale -= DevBoardSettings.SCALE_STEP);
				scaleRow.Button("+", () => Settings.Scale += DevBoardSettings.SCALE_STEP);
				scaleRow.Button("1x", () => Settings.Scale = 1f);

#if UNITY_EDITOR
				form.Toggle("Physical Size", () => Settings.KeepPhysicalSizeInEditor, value => Settings.KeepPhysicalSizeInEditor = value)
					.WithDescription("Scale by screen DPI. Editor only");
#endif

				var resetRow = page.Row().WithPadding(0).WithAlignment(LayoutAlignment.End);
				resetRow.Button("Reset options", Settings.ResetToDefaults).InteractiveOnly();
			}, "Display", int.MaxValue);
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
