
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.DebugKit
{
	[CreateAssetMenu(menuName = "Tekly/Debug Kit/Settings", fileName = "DebugKitSettings", order = 0)]
	public class DebugKitSettings : ScriptableObject
	{
		private const string DIRECTORY = "Assets/Resources/DebugKit/";
		private const string FILE = "debugkit_settings.asset";
		private const string PANEL_SETTINGS_FILE = "Packages/ca.tekly.debugkit/Assets/debug_kit_panel_settings.asset";

		public bool AutoScaleInEditor = true;
		
#if ENABLE_INPUT_SYSTEM
		public UnityEngine.InputSystem.Key OpenKey = UnityEngine.InputSystem.Key.F2;
		public UnityEngine.InputSystem.InputActionReference OpenAction;
#else
		public KeyCode OpenKey = KeyCode.F2;
#endif

		public int OpenTouchCount;
		
		public PanelSettings PanelSettings;
		public StyleSheet[] StyleSheets;
		public string[] RootClassNames;

#if UNITY_EDITOR
		public static DebugKitSettings CreateDefaultSettings()
		{
			var panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(PANEL_SETTINGS_FILE);
			var instance = CreateInstance<DebugKitSettings>();

			instance.PanelSettings = panelSettings;

			Directory.CreateDirectory(DIRECTORY);
			UnityEditor.AssetDatabase.CreateAsset(instance, DIRECTORY + FILE);
			return instance;
		}
#endif
	}
}