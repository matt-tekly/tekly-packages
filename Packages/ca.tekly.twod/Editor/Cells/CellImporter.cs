using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Tekly.TwoD.Common;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.TwoD.Cells
{
	[Serializable]
	public class CellImporterSettings : ITextureSettings
	{
		public TextureFormat TextureFormat => m_textureFormat;
		public TextureCompressionQuality CompressionQuality => m_textureCompressionQuality;
		public FilterMode FilterMode => m_filterMode;
		public int PixelsPerUnit => m_pixelsPerUnit;
		public Vector2 Pivot => m_pivot;
		
		[SerializeField] private TextureFormat m_textureFormat = TextureFormat.RGBA32;
		[SerializeField] private TextureCompressionQuality m_textureCompressionQuality = TextureCompressionQuality.Best;
		[SerializeField] private FilterMode m_filterMode = FilterMode.Point;
		
		[SerializeField] private int m_pixelsPerUnit = 100;
		[SerializeField] private Vector2 m_pivot = new Vector2(0.5f, 0f);

		public void Initialize()
		{
			EditorUtility.CopySerializedManagedFieldsOnly(TwoDDefaults.instance.Cells, this);
		}
	}
	
	[ScriptedImporter(9, "cell")]
	public class CellImporter : ScriptedImporter
	{
		[SerializeField] private CellImporterSettings m_settings;

		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (m_settings == null) {
				m_settings = new CellImporterSettings();
				m_settings.Initialize();
			}
			
			using var fileStream = File.OpenRead(ctx.assetPath);
			using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);

			var texture = ZipArchiveUtils.GetTexture(zipArchive, m_settings);
			ctx.AddObjectToAsset("texture", texture, texture);
			
			var asepriteData = ZipArchiveUtils.GetAsepriteData(zipArchive);
			var sprites = CellTextureImporter.GenerateSprites(texture, asepriteData, m_settings);

			foreach (var sprite in sprites) {
				ctx.AddObjectToAsset(sprite.name, sprite);
			}
			
			CellAnimationImporter.Import(sprites, asepriteData, ctx);
		}
	}
}