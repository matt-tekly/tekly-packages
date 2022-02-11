//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if (WEBSTER_ENABLE || UNITY_EDITOR && WEBSTER_ENABLE_EDITOR)
using System;
using System.IO;
using System.Threading;
using Tekly.Webster.FramelineCore;
using Tekly.Webster.Utility;

using UnityEngine;

namespace Tekly.Webster.Assets
{
	public static partial class AssetsEmbedded
	{
#if WEBSTER_DISABLE_FRONTEND
		public static void Initialize(AssetLoader assetLoader)
		{
			var index = System.Text.Encoding.UTF8.GetBytes("Webster frontend disabled");
			assetLoader.AddStaticAsset("index.html", index, false);
		}
#else
		public static void Initialize(AssetLoader assetLoader)
		{
			ThreadPool.QueueUserWorkItem(_ => {
				foreach (var asset in Assets) {
					var bytes = Convert.FromBase64String(asset.Value);
					assetLoader.AddStaticAsset(asset.Key, bytes, true);
				}
			});

			InitializeEditor(assetLoader);
		}

		private static void InitializeEditor(AssetLoader assetLoader)
		{
#if UNITY_EDITOR
			const string assetDirectory = "webster-viewer~/build/";

			// Hacky way to get the location of build directory.
			var config = ScriptableObject.CreateInstance<FramelineConfigAsset>();

			var monoScript = UnityEditor.MonoScript.FromScriptableObject(config);
			var monoScriptPath = UnityEditor.AssetDatabase.GetAssetPath(monoScript);

			var assetsPaths = Path.Combine(Path.Combine(monoScriptPath, "../../"), assetDirectory);

			assetLoader.AddAssetLoader(new DynamicAssetLoader(assetsPaths));
#endif
		}
#endif
	}
}

#endif