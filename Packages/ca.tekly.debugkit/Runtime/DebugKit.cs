using System;
using Tekly.Common.Utils;
using Tekly.DebugKit.Performance;
using Tekly.DebugKit.Utils;
using Tekly.DebugKit.Widgets;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Tekly.DebugKit
{
	public class DebugKit : Singleton<DebugKit>, IDisposable
	{
		public DebugKitSettings Settings => m_settings;
		public readonly DebugKitPreferences Preferences = new DebugKitPreferences();

		public float Scale
		{
			get => m_debugKitGui.Scale;
			set => m_debugKitGui.Scale = Mathf.Clamp(value, 0.1f, 10f);
		}
		
		private DebugKitGui m_debugKitGui;
		private PerformanceMonitor m_performanceMonitor;
		private bool m_inputConsumed;

		private DebugKitSettings m_settings;
		private DebugKitRoot m_debugKitRoot;

#if DEBUGKIT_DISABLED
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public void Initialize(DebugKitSettings settings = null)
		{
			m_settings = settings ?? LoadSettings();

			var go = new GameObject("DebugKit");
			Object.DontDestroyOnLoad(go);

			m_debugKitGui = go.AddComponent<DebugKitGui>();
			m_debugKitGui.Initialize(this);
			
			m_debugKitRoot = new DebugKitRoot(m_debugKitGui.Root);;
			
			m_performanceMonitor = new PerformanceMonitor(m_debugKitGui.Root, this);
			
			SetupInputActions();
			
			Enable(false);
		}

		public Menu Menu(string name, string classNames = null)
		{
			return m_debugKitRoot.Menu(name, classNames);	
		}

		public void RemoveMenu(Menu menu)
		{
			m_debugKitRoot.RemoveMenu(menu);
		}

		public void Open()
		{
			m_debugKitRoot.Enabled = true;
		}
		
		public void Close()
		{
			m_debugKitRoot.Enabled = false;
		}

		public void Enable(bool enabled)
		{
			m_debugKitRoot.Enabled = enabled;
		}

		public void Update()
		{
			if (WasToggleButtonPressed()) {
				m_debugKitRoot.Toggle();
			}

#if UNITY_EDITOR
			if (Settings.AutoScaleInEditor && Preferences.ScaleInEditor.Value) {
				Scale = DebugKitScreen.ViewScale();
			} else {
				Scale = 1;
			}
#endif
			if (Preferences.ScaleOverrideEnabled.Value) {
				Scale = Preferences.ScaleOverride.Value;
			}
			
			m_debugKitRoot.Update();
			m_performanceMonitor.Update();
		}

		private DebugKitSettings LoadSettings()
		{
			var settings = Resources.LoadAll<DebugKitSettings>("");

			if (settings.Length > 0) {
				return settings[0];
			}

#if UNITY_EDITOR
			Debug.LogWarning("Failed to find DebugKitSettings. Creating a default one.");
			return DebugKitSettings.CreateDefaultSettings();
#else
			Debug.LogError("Failed to find DebugKitSettings. Please create one in the Resources folder.");
			return new DebugKitSettings();
#endif
		}
		
		private void SetupInputActions()
		{
#if ENABLE_INPUT_SYSTEM
			if (Settings.OpenAction != null) {
				Settings.OpenAction.action.performed += _ => m_debugKitRoot.Toggle();
			}

			if (Settings.OpenTouchCount > 0) {
				UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
				UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
#endif
			}
#endif
		}

		private bool WasToggleButtonPressed()
		{
#if ENABLE_INPUT_SYSTEM
			if (Settings.OpenAction != null) {
				return false;
			}
			
			var toggle = UnityEngine.InputSystem.Keyboard.current[Settings.OpenKey].wasPressedThisFrame ||
			             (Settings.OpenTouchCount > 0 && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == Settings.OpenTouchCount);
#else
			var toggle = Input.GetKeyDown(Settings.OpenKey) ||
			             (Settings.OpenTouchCount > 0 && Input.touchCount == DebugKitSettings.TOUCH_COUNT);
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
			m_debugKitRoot = null;
			m_debugKitGui = null;
			m_inputConsumed = false;
		}
	}
}
