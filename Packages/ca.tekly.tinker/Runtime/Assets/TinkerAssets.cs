#if UNITY_EDITOR
#define TINKER_ENABLED
#endif

using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Tinker.Assets
{
	[Serializable]
	public class TinkerAsset
	{
		public Object Asset;
		public string Name;
		public string Url;
		public bool IsTemplate;
	}

	[CreateAssetMenu(menuName = "Tinker Assets", fileName = "tinker_assets", order = 0)]
	public class TinkerAssets : ScriptableObject
	{
#if UNITY_EDITOR
		[Tooltip("All assets in this directory will be served by the path relative to this directory")] [SerializeField]
		private Object m_directory;
#endif

#if TINKER_ENABLED
		public TinkerAsset[] Assets;
#endif

#if UNITY_EDITOR && TINKER_ENABLED
		private void OnValidate()
		{
			var directory = UnityEditor.AssetDatabase.GetAssetPath(m_directory);

			Assets = UnityEditor.AssetDatabase.FindAssets("t:Object", new[] { directory })
				.Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
				.Select(path => (path, asset: UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(path)))
				.Where(pathAndAsset => pathAndAsset.asset is not UnityEditor.DefaultAsset)
				.Select(pathAndAsset => {
					var (path, asset) = pathAndAsset;
					return new TinkerAsset {
						Url = path.Replace(directory, ""),
						Name = asset.name,
						IsTemplate = asset is TextAsset && path.Contains(".liquid"),
						Asset = asset
					};
				})
				.ToArray();
		}
#endif
	}
}