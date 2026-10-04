using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Pages;
using Tekly.DevBoard.Panels;
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
		/// Ticks behaviours that aren't inside a Board.
		/// </summary>
		internal TickGroup UnboardedTickGroup {
			get {
				Initialize();
				return m_ticker.TickGroup;
			}
		}

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

		public Board Board(string name, string variant = "board")
		{
			Initialize();

			var board = Object.Instantiate(Get<Board>(variant), m_root.transform);
			board.name = name;

			return board;
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

		internal DevBoardPanel PopOut(DevBoardPanel source)
		{
			var number = 1;

			while (FindPanel($"popout-{number}") != null) {
				number++;
			}

			return CreatePanel(new PanelRecord {
				Id = $"popout-{number}",
				Dock = DockSlot.TopRight,
				Path = source.Path,
				Overlay = true
			});
		}

		/// <summary>
		/// A panel was closed by the user: forget it, including its saved layout.
		/// </summary>
		internal void ClosePanel(DevBoardPanel panel)
		{
			m_panels.Remove(panel);
			SavePanels();
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

		private DevBoardPanel CreatePanel(PanelRecord record)
		{
			Initialize();

			// Unity null check on the canvas, so a destroyed dock is recreated along with the root
			if (m_dock == null || m_dock.Canvas == null) {
				m_dock = new DevBoardDock(m_root.transform);
				m_dock.ApplySettings(Settings);
			}

			if (!TryGet(PANEL_VARIANT, out DevBoardPanel prefab)) {
				WarnOnce($"[DevBoard] No '{PANEL_VARIANT}' widget is registered, so panels can't be created. Add the panel prefab to a DevBoardAssets (Collect Assets).", true);
				return null;
			}

			var panel = DevBoardPanel.Create(this, m_dock, prefab, record);
			m_panels.Add(panel);
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

		private void RegisterOptionsPage()
		{
			Page(OPTIONS_PATH, page => {
				var form = page.Root.Form();
				form.FloatInput("UI Scale", () => Settings.Scale, value => Settings.Scale = value)
					.WithFormat("0.##");

				var scaleRow = form.Row().WithPadding(0);
				scaleRow.Button("-", () => Settings.Scale -= DevBoardSettings.SCALE_STEP);
				scaleRow.Button("+", () => Settings.Scale += DevBoardSettings.SCALE_STEP);
				scaleRow.Button("1x", () => Settings.Scale = 1f);

#if UNITY_EDITOR
				form.Toggle("Use Physical Size Scale", () => Settings.KeepPhysicalSizeInEditor,
					value => Settings.KeepPhysicalSizeInEditor = value);
#endif

				form.Button("Reset options", Settings.ResetToDefaults);
			}, order: int.MinValue);
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
