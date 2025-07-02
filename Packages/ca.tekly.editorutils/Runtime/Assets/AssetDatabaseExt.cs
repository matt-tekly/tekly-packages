#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Tekly.EditorUtils.Assets
{
	public static class AssetDatabaseExt
	{
		public static T[] FindAndLoad<T>(string[] searchInFolders = null) where T : Object
		{
			return FindAndLoad<T>($"t:{typeof(T).Name}", searchInFolders);
		}

		public static T[] FindAndLoad<T>(string search, string[] searchInFolders = null) where T : Object
		{
			return AssetDatabase.FindAssets(search, searchInFolders)
				.SelectMany(guid => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
				.OfType<T>()
				.Distinct()
				.ToArray();
		}

		public static Object[] FindAndLoad(Type type, string search, string[] searchDirectories = null)
		{
			return AssetDatabase.FindAssets(search, searchDirectories)
				.SelectMany(guid => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
				.Where(x => x != null && type.IsInstanceOfType(x))
				.Distinct()
				.ToArray();
		}

		public static T[] FindAndLoad<T>(string search, params Object[] searchInObjectDirs) where T : Object
		{
			var directories = searchInObjectDirs.Select(asset => {
				var path = AssetDatabase.GetAssetPath(asset);
				return Path.GetDirectoryName(path);
			}).ToArray();

			return FindAndLoad<T>(search, directories);
		}

		public static T FindAndLoadFirst<T>(string name) where T : Object
		{
			return FindAndLoad<T>(name)
				.Where(x => x != null)
				.FirstOrDefault(x => x.name == name);
		}

		public static Object FindAndLoadFirst(string name, Type type)
		{
			return FindAndLoad(type, name)
				.Where(x => x != null)
				.FirstOrDefault(x => x.name == name);
		}

		public static T LoadOrCreate<T>(string path) where T : ScriptableObject
		{
			return LoadOrCreate(path, typeof(T)) as T;
		}

		public static ScriptableObject LoadOrCreate(string path, Type type)
		{
			Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(type));
			var result = AssetDatabase.LoadAssetAtPath(path, type) as ScriptableObject;

			if (result == null) {
				var directory = Path.GetDirectoryName(path);
				Directory.CreateDirectory(directory);

				result = ScriptableObject.CreateInstance(type);
				AssetDatabase.CreateAsset(result, path);
			} else {
				EditorUtility.SetDirty(result);
			}

			return result;
		}
	}
}
#endif