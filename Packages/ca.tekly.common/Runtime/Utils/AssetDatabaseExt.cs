#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Common.Utils
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
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<T>)
				.Where(x => x != null)
				.ToArray();
		}
		
		public static Object[] FindAndLoad(Type type, string search, string[] searchInFolders = null)
		{
			return AssetDatabase.FindAssets(search, searchInFolders)
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(x => AssetDatabase.LoadAssetAtPath(x, type))
				.Where(x => x != null)
				.ToArray();
		}

		public static T[] FindAndLoad<T>(string search, params Object[] searchInObjectDirs) where T : Object
		{
			var directories = searchInObjectDirs.Select(AssetDatabase.GetAssetPath).ToArray();

			return FindAndLoad<T>(search, directories);
		}
		
		public static T FindAndLoadFirst<T>(string name) where T : Object
		{
			return FindAndLoad<T>(name).FirstOrDefault();
		}
		
		public static Object FindAndLoadFirst(string name, Type type)
		{
			return FindAndLoad(type, name).FirstOrDefault();
		}
		
		public static T LoadOrCreate<T>(string path) where T : ScriptableObject
		{
			var result = AssetDatabase.LoadAssetAtPath<T>(path);
			
			if (result == null) {
				var directory = Path.GetDirectoryName(path);
				Directory.CreateDirectory(directory);
				
				result = ScriptableObject.CreateInstance<T>();
				AssetDatabase.CreateAsset(result, path);
			}

			return result;
		}
	}
}
#endif