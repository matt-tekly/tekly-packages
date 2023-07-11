using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.TwoD.Cells
{
	[Serializable]
	public class CellImporterSettings
	{
		public TextureFormat TextureFormat = TextureFormat.RGBA32;
		public TextureCompressionQuality TextureCompressionQuality = TextureCompressionQuality.Best;
		public FilterMode FilterMode = FilterMode.Point;
		
		public int PixelsPerUnit = 100;
		public Vector2 Pivot = new Vector2(0.5f, 0f);
	}
	
	[ScriptedImporter(9, "cell")]
	public class CellImporter : ScriptedImporter
	{
		[SerializeField] private CellImporterSettings m_settings;

		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (m_settings == null) {
				m_settings = new CellImporterSettings();
			}
			
			var fileStream = File.OpenRead(ctx.assetPath);
			var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);

			var textureEntry = zipArchive.Entries.First(x => x.FullName.Contains(".png"));
			
			var aseSpriteData = GetAseSpriteData(zipArchive);
			var texture = CellTextureImporter.CreateTexture(textureEntry, m_settings);
			ctx.AddObjectToAsset("texture", texture, texture);
			
			var sprites = CellTextureImporter.GenerateSprites(texture, aseSpriteData, m_settings);

			foreach (var sprite in sprites) {
				ctx.AddObjectToAsset(sprite.name, sprite);
			}
			
			CellAnimationImporter.Import(sprites, aseSpriteData, ctx);
		}

		private static AseSpriteData GetAseSpriteData(ZipArchive zipArchive)
		{
			var jsonEntry = zipArchive.Entries.First(x => x.FullName.Contains(".json"));
			
			using var entryStream = jsonEntry.Open();
			using var reader = new StreamReader(entryStream);
			var json = reader.ReadToEnd();
			
			return JsonUtility.FromJson<AseSpriteData>(json);
		}
	}
}