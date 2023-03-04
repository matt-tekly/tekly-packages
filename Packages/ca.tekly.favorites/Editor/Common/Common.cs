using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.Favorites.Common
{
	internal static class CommonUtils
	{
		private static string s_packageDirectory;

		public static string PackageDirectory {
			get {
				if (s_packageDirectory == null) {
					var instance = ScriptableObject.CreateInstance<FavoritesData>();
					var path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(instance));
					Object.DestroyImmediate(instance);

					s_packageDirectory = path.Replace("Editor/Core/Data/FavoritesData.cs", "");
				}

				return s_packageDirectory;
			}
		}

		public static VisualTreeAsset Uxml(string path)
		{
			return LoadAssetAtPath<VisualTreeAsset>(path);
		}

		public static StyleSheet Uss(string path)
		{
			return LoadAssetAtPath<StyleSheet>(path);
		}

		public static Texture2D Texture(string path)
		{
			return LoadAssetAtPath<Texture2D>(path);
		}

		public static T LoadAssetAtPath<T>(string path) where T : Object
		{
			var asset = AssetDatabase.LoadAssetAtPath<T>(PackageDirectory + path);
			if (asset == null) {
				Debug.LogError($"Failed to find {typeof(T).Name} [{PackageDirectory + path}]");
			}

			return asset;
		}
	}
}