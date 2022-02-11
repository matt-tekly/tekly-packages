//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System.IO;
using System.Threading;

namespace Tekly.Webster.Utility
{
	/// <summary>
	/// Loads assets from a directory
	/// </summary>
	public class DynamicAssetLoader : IAssetLoader
	{
		private readonly string m_assetDirectory;

		public DynamicAssetLoader(string directory)
		{
			m_assetDirectory = Path.GetFullPath(directory);
		}

		public bool TryGetAsset(string file, out byte[] bytes)
		{
			var path = FullPath(file);

			if (File.Exists(path)) {
				bytes = File.ReadAllBytes(path);
				return true;
			}

			// This is a hack to wait for hot module reloads occurring in the web frontend
			if (file.EndsWith(".js")) {
				long sleepTime = 2000;

				while (sleepTime > 0) {
					Thread.Sleep(200);
					sleepTime -= 200;

					if (File.Exists(path)) {
						bytes = File.ReadAllBytes(path);
						return true;
					}
				}
			}

			bytes = null;
			return false;
		}

		private string FullPath(string file)
		{
			return Path.Combine(m_assetDirectory, file.TrimStart('/', '\\'));
		}
	}
}