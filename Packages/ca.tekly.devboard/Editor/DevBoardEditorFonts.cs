using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Registers the fonts and materials from every DevBoardAssets in the project while in the editor,
	/// so <font> and <material> tags (and styles using them) render outside of play mode.
	/// </summary>
	[InitializeOnLoad]
	public static class DevBoardEditorFonts
	{
		static DevBoardEditorFonts()
		{
			// The AssetDatabase isn't reliable inside InitializeOnLoad, so wait a tick
			EditorApplication.delayCall += RegisterAll;
			EditorApplication.projectChanged += RegisterAll;
		}

		public static void RegisterAll()
		{
			foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(DevBoardAssets)}")) {
				var assets = AssetDatabase.LoadAssetAtPath<DevBoardAssets>(AssetDatabase.GUIDToAssetPath(guid));
				DevBoardFonts.Register(assets);
			}

			RefreshTexts();
		}

		/// <summary>
		/// Text parsed before registration fell back to the default font, so reparse everything visible.
		/// </summary>
		private static void RefreshTexts()
		{
			var texts = new List<TMP_Text>(Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None));

			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

			if (prefabStage != null) {
				texts.AddRange(prefabStage.prefabContentsRoot.GetComponentsInChildren<TMP_Text>(true));
			}

			foreach (var text in texts) {
				text.havePropertiesChanged = true;
			}
		}
	}
}
