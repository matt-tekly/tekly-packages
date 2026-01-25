//=============================================================================
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Tekly.Webster.Utility
{
	public static class WebsterMenus
	{
		private static readonly NamedBuildTarget[] s_buildTargets = {
			NamedBuildTarget.Android,
			NamedBuildTarget.iOS,
			NamedBuildTarget.PS4,
			NamedBuildTarget.Standalone,
			NamedBuildTarget.XboxOne,
			NamedBuildTarget.NintendoSwitch,
			NamedBuildTarget.NintendoSwitch2,
			NamedBuildTarget.WebGL
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
		public static void DisableWebsterEditMode()
		{
			DisableDefine(WEBSTER_ENABLE_EDIT_MODE);
		}
#endif

		public static void EnableDefine(string define)
		{
			foreach (var buildTarget in s_buildTargets) {
				var defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);

				defines = AddDefine(defines, define);

				PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
			}
		}

		public static void DisableDefine(string define)
		{
			foreach (var buildTarget in s_buildTargets) {
				var defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
				defines = RemoveDefine(defines, define);

				PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
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