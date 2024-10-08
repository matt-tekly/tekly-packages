using System;
using System.IO;
using Tekly.EditorUtils.Assets;
using UnityEditor;
using UnityEngine;

namespace Tekly.BasicBuilder
{
	[CreateAssetMenu(menuName = "Tekly/Basic Builder/Settings")]
	public class BuildWindowSettings : ScriptableObject
	{
		public BuildPlatformSettings[] Platforms;
		public BuilderDefineSetting[] Defines;
		public bool UseAddressables;
		
		private const string GAME_SETTINGS = "BuildWindowSettings";
		private const string GAME_SETTINGS_DIR = "Assets/Editor/Builder";
		private const string GAME_SETTINGS_FILE = GAME_SETTINGS_DIR + "/" + GAME_SETTINGS + ".asset";
			
		public static BuildWindowSettings GetSettings()
		{
			var buildWindowSettings = AssetDatabaseExt.FindAndLoad<BuildWindowSettings>("BuildWindowSettings");

			foreach (var buildWindowSetting in buildWindowSettings) {
				if (buildWindowSetting.name == GAME_SETTINGS) {
					return buildWindowSetting;
				}
			}

			var settings = Instantiate(buildWindowSettings[0]);
			Directory.CreateDirectory(GAME_SETTINGS_DIR);
			AssetDatabase.CreateAsset(settings, GAME_SETTINGS_FILE);

			return settings;
		}
	}

	[Serializable]
	public class BuildPlatformSettings
	{
		public BuildTarget BuildTarget;
		public BuildTargetGroup BuildTargetGroup;
		public GUIContent Content;
	}
	
	[Serializable]
	public class BuilderDefineSetting
	{
		public string DisplayName;
		public string Define;
		public string ToolTip;
	}
}