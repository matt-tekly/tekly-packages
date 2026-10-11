using System;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace TeklySample.Editor
{
	[CustomEditor(typeof(TMP_FontAsset))]
	public class FontAssetSaveDisabler : TMP_FontAssetEditor
	{
		private static bool FontAssetsLocked
		{
			get => EditorPrefs.GetBool("TMPFontsLocked", true);
			set => EditorPrefs.SetBool("TMPFontsLocked", value);
		}

		public override void OnInspectorGUI()
		{
			if (GUILayout.Button(FontAssetsLocked ? "Unlock Font Assets" : "Lock Font Assets"))
			{
				FontAssetsLocked = !FontAssetsLocked;
				GUIUtility.ExitGUI();
			}

			if (!FontAssetsLocked)
			{
				EditorGUILayout.Space();
				base.OnInspectorGUI();
			}
		}

		private class SaveHandler : AssetModificationProcessor
		{
			private static string[] OnWillSaveAssets(string[] paths)
			{
				if (FontAssetsLocked)
				{
					int index = 0;
					foreach (string path in paths)
					{
						if (!typeof(TMP_FontAsset).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path)))
							paths[index++] = path;
					}

					Array.Resize(ref paths, index);
				}

				return paths;
			}
		}
	}
}