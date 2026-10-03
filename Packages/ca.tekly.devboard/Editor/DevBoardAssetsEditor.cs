using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Collects assets for a DevBoardAssets from its folder and every folder beneath it.
	/// Every public array field is filled by type, so new widget types are picked up without changes here.
	/// </summary>
	[CustomEditor(typeof(DevBoardAssets))]
	public class DevBoardAssetsEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			if (GUILayout.Button("Collect Assets")) {
				Collect((DevBoardAssets) target);
			}

			EditorGUILayout.Space();
			base.OnInspectorGUI();
		}

		public static void Collect(DevBoardAssets assets)
		{
			var folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(assets))?.Replace('\\', '/');

			if (string.IsNullOrEmpty(folder)) {
				return;
			}

			Undo.RecordObject(assets, "Collect DevBoard Assets");

			var fields = typeof(DevBoardAssets).GetFields(BindingFlags.Public | BindingFlags.Instance);

			foreach (var field in fields) {
				var elementType = field.FieldType.GetElementType();

				if (!field.FieldType.IsArray || elementType == null || !typeof(Object).IsAssignableFrom(elementType)) {
					continue;
				}

				var found = typeof(Component).IsAssignableFrom(elementType)
					? FindPrefabComponents(elementType, folder)
					: FindMainAssets(elementType, folder);

				var merged = Merge(field.GetValue(assets) as Array, found);
				var array = Array.CreateInstance(elementType, merged.Count);

				for (var i = 0; i < merged.Count; i++) {
					array.SetValue(merged[i], i);
				}

				field.SetValue(assets, array);
			}

			EditorUtility.SetDirty(assets);
			AssetDatabase.SaveAssetIfDirty(assets);

			DevBoardEditorFonts.RegisterAll();
		}

		/// <summary>
		/// Keeps existing entries in their current order (the first widget of each type is that type's default),
		/// drops missing references and appends anything newly found.
		/// </summary>
		private static List<Object> Merge(Array existing, List<Object> found)
		{
			var merged = new List<Object>();

			if (existing != null) {
				foreach (var item in existing) {
					var asset = item as Object;

					if (asset != null && !merged.Contains(asset)) {
						merged.Add(asset);
					}
				}
			}

			foreach (var asset in found) {
				if (!merged.Contains(asset)) {
					merged.Add(asset);
				}
			}

			return merged;
		}

		/// <summary>
		/// Finds prefabs whose root has a component of this type or a subclass of it, e.g. every widget prefab
		/// for the Widget array. Only the root is checked, so widgets nested inside other widgets aren't collected.
		/// </summary>
		private static List<Object> FindPrefabComponents(Type componentType, string folder)
		{
			var results = new List<Object>();

			foreach (var path in FindPaths("t:Prefab", folder)) {
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				if (prefab == null) {
					continue;
				}

				var component = prefab.GetComponents<Component>()
					.FirstOrDefault(c => c != null && componentType.IsInstanceOfType(c));

				if (component != null) {
					results.Add(component);
				}
			}

			return results;
		}

		/// <summary>
		/// Finds main assets of this type. Sub-assets are skipped, so the material embedded
		/// in a TMP font asset isn't collected as a font material.
		/// </summary>
		private static List<Object> FindMainAssets(Type assetType, string folder)
		{
			var results = new List<Object>();

			foreach (var path in FindPaths($"t:{assetType.Name}", folder)) {
				var asset = AssetDatabase.LoadMainAssetAtPath(path);

				if (asset != null && assetType.IsInstanceOfType(asset)) {
					results.Add(asset);
				}
			}

			return results;
		}

		private static IEnumerable<string> FindPaths(string filter, string folder)
		{
			return AssetDatabase.FindAssets(filter, new[] { folder })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Distinct()
				.OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);
		}
	}
}
