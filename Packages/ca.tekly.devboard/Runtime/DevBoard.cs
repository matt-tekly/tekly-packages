using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DevBoard.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.DevBoard
{
	public class DevBoard : Singleton<DevBoard>
	{
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

		// Widget prefabs by name (the variant), and the first prefab of each exact type as that type's fallback
		private readonly Dictionary<string, Widget> m_widgets = new();
		private readonly Dictionary<Type, Widget> m_defaults = new();
		private readonly HashSet<string> m_warnings = new();

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

		public Board Board(string name, string variant = "board")
		{
			Initialize();

			var board = Object.Instantiate(Get<Board>(variant), m_root.transform);
			board.name = name;

			return board;
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

		private void WarnOnce(string message)
		{
			if (m_warnings.Add(message)) {
				Debug.LogWarning(message);
			}
		}
	}
}
