//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Tekly.Webster.Utility
{
	public interface IAssetLoader
	{
		bool TryGetAsset(string file, out byte[] bytes);
	}

	public struct GetResourceResult
	{
		public string Resource;
		public bool Found;
		public byte[] Bytes;
		public bool Zipped;
	}

	public class StaticAsset
	{
		public byte[] Content;
		public bool IsZipped;
		public string Path;
	}

	public class AssetLoader
	{
		private readonly ConcurrentDictionary<string, StaticAsset> m_staticAssets = new ConcurrentDictionary<string, StaticAsset>();
		private readonly List<IAssetLoader> m_assetLoaders = new List<IAssetLoader>();

		public void AddStaticAsset(string path, byte[] content, bool isZipped)
		{
			var actualPath = $"/{path}";
			m_staticAssets[actualPath] = new StaticAsset {
				Path = actualPath,
				Content = content,
				IsZipped = isZipped
			};
		}

		public void AddAssetLoader(IAssetLoader loader)
		{
			m_assetLoaders.Insert(0, loader);
		}

		public GetResourceResult GetResource(string path)
		{
			var result = new GetResourceResult();
			result.Resource = path;

			foreach (var loader in m_assetLoaders) {
				if (loader.TryGetAsset(path, out var bytes)) {
					result.Found = true;
					result.Bytes = bytes;
					result.Zipped = false;
					return result;
				}
			}

			if (m_staticAssets.TryGetValue(path, out var asset)) {
				result.Found = true;
				result.Bytes = asset.Content;
				result.Zipped = asset.IsZipped;
			}

			return result;
		}
	}
}