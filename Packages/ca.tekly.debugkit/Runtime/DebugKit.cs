using System;
using Tekly.Common.Utils;
using Tekly.DebugKit.Performance;
using Tekly.DebugKit.Utils;
using Tekly.DebugKit.Widgets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.DebugKit
{
	public class DebugKit : Singleton<DebugKit>, IDisposable
	{
		private DebugKitGui m_debugKitGui;
		private MenuController m_menuController;
		private bool m_inputConsumed;
		private PerformanceMonitor m_performanceMonitor;

		public DebugKitSettings Settings;

		public float Scale {
			get => m_debugKitGui.Scale;
			set => m_debugKitGui.Scale = value;
		}

		public void Initialize(DebugKitSettings settings = null)
		{
			if (settings == null) {
				Settings = LoadSettings();
			}

			var go = new GameObject("DebugKit");
			m_debugKitGui = go.AddComponent<DebugKitGui>();
			m_debugKitGui.Initialize(this);

			Object.DontDestroyOnLoad(go);

			m_menuController = new MenuController(m_debugKitGui.Root);

#if ENABLE_INPUT_SYSTEM
			UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
			UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
#endif
#endif

			m_performanceMonitor = new PerformanceMonitor(m_debugKitGui.Root, this);
		}

		public Container Menu(string name, string classNames = null)
		{
			return m_menuController.Create(name, classNames);
		}

		public void Update()
		{
			if (WasToggleButtonPressed()) {
				m_menuController.Toggle();
			}

			Scale = DebugKitScreen.ViewScale();
			
			m_menuController.Update();
			m_performanceMonitor.Update();
		}

		public DebugKitSettings LoadSettings()
		{
			var settings = Resources.LoadAll<DebugKitSettings>("");

			if (settings.Length > 0) {
				return settings[0];
			}

#if UNITY_EDITOR
			Debug.LogWarning("Failed to find DebugKitSettings. Creating a default one");
			return DebugKitSettings.CreateDefaultSettings();
#else
			Debug.LogError("Failed to find DebugKitSettings. Please create one in the Resources folder.");
			return new DebugKitSettings();
#endif
		}

		private bool WasToggleButtonPressed()
		{
			var toggle =
#if ENABLE_INPUT_SYSTEM
				UnityEngine.InputSystem.Keyboard.current[Settings.OpenKey].wasPressedThisFrame ||
				(Settings.OpenTouchCount > 0 && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == Settings.OpenTouchCount);

#else
				Input.GetKeyDown(Settings.OpenKey) ||
				Input.touchCount == DebugKitSettings.TOUCH_COUNT;
#endif

			m_inputConsumed &= toggle;

			if (toggle && !m_inputConsumed) {
				m_inputConsumed = true;
				return true;
			}

			return false;
		}

		public void Dispose()
		{
			m_performanceMonitor?.Dispose();
		}
	}
}