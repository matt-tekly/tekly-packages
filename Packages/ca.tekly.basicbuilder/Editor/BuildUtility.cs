using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Tekly.BasicBuilder
{
	public static class BuildUtility
	{
		public static string ApplicationName => PlayerSettings.productName;
		public static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
		
		public static void BuildAddressables()
		{
			AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
			AddressableAssetSettings.BuildPlayerContent();
		}

		public static void SwitchAddressablesProfile(string profileName)
		{
			var settings = AddressableAssetSettingsDefaultObject.Settings;
			var profileID = settings.profileSettings.GetProfileId(profileName);
			settings.activeProfileId = profileID;
		}
		
		public static string GetActiveAddressablesProfileName()
		{
			var settings = AddressableAssetSettingsDefaultObject.Settings;
			return settings.profileSettings.GetProfileName(settings.activeProfileId);
		}
		
		public static List<string> GetAddressableProfileNames()
		{
			var settings = AddressableAssetSettingsDefaultObject.Settings;
			return settings.profileSettings.GetAllProfileNames();
		}
		
		public static string GetTargetSimpleName(BuildTarget buildTarget)
		{
			switch (buildTarget) {
				case BuildTarget.StandaloneOSX: return "macos";
				case BuildTarget.StandaloneWindows64: return "windows";
				case BuildTarget.iOS: return "ios";
				case BuildTarget.Android: return "android";
				default: throw new Exception("Unsupported Build Target: " + buildTarget);
			}
		}
		
		public static string GetLocationPathName(BuildTarget buildTarget)
		{
			switch (buildTarget) {
				case BuildTarget.StandaloneOSX: return ApplicationName;
				case BuildTarget.StandaloneWindows64: return $"{ApplicationName}.exe";
				case BuildTarget.iOS: return ApplicationName;
				case BuildTarget.Android: return $"{ApplicationName}.apk";
				default: throw new Exception("Unsupported Build Target: " + buildTarget);
			}
		}
	}
}