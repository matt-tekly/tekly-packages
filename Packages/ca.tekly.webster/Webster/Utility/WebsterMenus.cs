//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tekly.Webster.Utility
{
	public static class WebsterMenus
	{
		private static readonly BuildTargetGroup[] s_buildTargetGroups = {
			BuildTargetGroup.Android,
			BuildTargetGroup.iOS,
			BuildTargetGroup.PS4,
			BuildTargetGroup.Standalone,
			BuildTargetGroup.XboxOne,
			BuildTargetGroup.Switch
		};

		private const string WEBSTER_ENABLE = "WEBSTER_ENABLE";
		private const string WEBSTER_ENABLE_EDITOR = "WEBSTER_ENABLE_EDITOR";
		private const string WEBSTER_ENABLE_EDIT_MODE = "WEBSTER_ENABLE_EDIT_MODE";

		[MenuItem("Tools/Tekly/Webster/Open Local", priority = 0)]
		public static void OpenLocal()
		{
			Application.OpenURL(string.Format("http://localhost:{0}", WebsterServer.HttpPort));
		}

#if !WEBSTER_ENABLE
		[MenuItem("Tools/Tekly/Webster/Enable Webster", priority = 100)]
		public static void EnableWebster()
		{
			EnableDefine(WEBSTER_ENABLE);
		}
#endif

#if WEBSTER_ENABLE
		[MenuItem("Tools/Tekly/Webster/Disable Webster", priority = 100)]
		public static void DisableWebster()
		{
			DisableDefine(WEBSTER_ENABLE);
		}
#endif

#if !WEBSTER_ENABLE_EDITOR
		[MenuItem("Tools/Tekly/Webster/Enable Webster Editor", priority = 100)]
		public static void EnableWebsterEditor()
		{
			EnableDefine(WEBSTER_ENABLE_EDITOR);
		}
#endif

#if WEBSTER_ENABLE_EDITOR
		[MenuItem("Tools/Tekly/Webster/Disable Webster Editor", priority = 100)]
		public static void DisableWebsterEditor()
		{
			DisableDefine(WEBSTER_ENABLE_EDITOR);
		}
#endif
		
#if !WEBSTER_ENABLE_EDIT_MODE
		[MenuItem("Tools/Tekly/Webster/Enable Webster Edit Mode", priority = 101)]
		public static void EnableWebsterEditMode()
		{
			EnableDefine(WEBSTER_ENABLE_EDIT_MODE);
		}
#endif

#if WEBSTER_ENABLE_EDIT_MODE
		[MenuItem("Tools/Tekly/Webster/Disable Webster Edit Mode", priority = 101)]
		public static void DisableWebsterEditor()
		{
			DisableDefine(WEBSTER_ENABLE_EDIT_MODE);
		}
#endif

		public static void EnableDefine(string define)
		{
			foreach (var buildTargetGroup in s_buildTargetGroups) {
				var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

				defines = AddDefine(defines, define);

				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
			}
		}

		public static void DisableDefine(string define)
		{
			foreach (var buildTargetGroup in s_buildTargetGroups) {
				var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
				defines = RemoveDefine(defines, define);

				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
			}
		}

		private static string AddDefine(string defines, string define)
		{
			if (defines.Split(';').Contains(define)) {
				return defines;
			}

			return defines + ";" + define;
		}

		private static string RemoveDefine(string defines, string define)
		{
			var definesArray = defines.Split(';').Where(def => def != define).ToArray();
			return string.Join(";", definesArray);
		}
	}
}
#endif