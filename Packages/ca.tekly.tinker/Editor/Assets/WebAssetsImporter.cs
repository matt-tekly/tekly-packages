using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;

namespace Tekly.Tinker.Assets
{
	[ScriptedImporter(1, new[] { "css", "jst", "liquid" }, new[] { "js" })]
	public class WebAssetsImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			var textAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
			ctx.AddObjectToAsset("main", textAsset);
			ctx.SetMainObject(textAsset);
		}
	}
}