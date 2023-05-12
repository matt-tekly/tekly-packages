#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

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

		public static T[] FindAndLoad<T>(string search, params Object[] searchInObjectDirs) where T : Object
		{
			var directories = searchInObjectDirs.Select(AssetDatabase.GetAssetPath).ToArray();

			return FindAndLoad<T>(search, directories);
		}
	}
}
#endif