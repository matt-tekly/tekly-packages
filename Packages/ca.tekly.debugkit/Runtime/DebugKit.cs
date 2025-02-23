using Tekly.Common.Utils;
using Tekly.DebugKit.Utils;
using Tekly.DebugKit.Widgets;
using UnityEngine;

namespace Tekly.DebugKit
{
	public class DebugKit : Singleton<DebugKit>
	{
		public bool Visible { get; set; }

		private DebugKitGui m_debugKitGui;
		private MenuController m_menuController;

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
		}

		public Menu Menu(string name, string classNames = null)
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
		}

		public DebugKitSettings LoadSettings()
		{
			var settings = Resources.LoadAll<DebugKitSettings>("");

			if (settings.Length > 0) {
				return settings[0];
			}

#if UNITY_EDITOR
			Debug.LogError("Failed to find DebugKitSettings. Creating a default one");
			return DebugKitSettings.CreateDefaultSettings();
#else
			Debug.LogError("Failed to find DebugKitSettings. Please create one in the Resources folder.");
			return new DebugKitSettings();
#endif
		}

		private bool WasToggleButtonPressed()
		{
#if ENABLE_INPUT_SYSTEM
			return UnityEngine.InputSystem.Keyboard.current[Settings.OpenKey].wasPressedThisFrame;
#else
            return Input.GetKeyDown(Settings.OpenKey);
#endif
		}
	}
}