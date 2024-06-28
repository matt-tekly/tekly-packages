using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Tekly.TwoD.Common;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tekly.TwoD.Tiles
{
	[Serializable]
	public class TilesImporterSettings : ITextureSettings
	{
		public TextureFormat TextureFormat => m_textureFormat;
		public TextureCompressionQuality CompressionQuality => m_textureCompressionQuality;
		public FilterMode FilterMode => m_filterMode;
		public int PixelsPerUnit => m_pixelsPerUnit;
		public bool GenerateTiles => m_generateTiles;
		
		[SerializeField] private TextureFormat m_textureFormat = TextureFormat.RGBA32;
		[SerializeField] private TextureCompressionQuality m_textureCompressionQuality = TextureCompressionQuality.Best;
		[SerializeField] private FilterMode m_filterMode = FilterMode.Point;
		
		[SerializeField] private int m_pixelsPerUnit = 100;
		[SerializeField] private bool m_generateTiles = true;

		public void Initialize()
		{
			EditorUtility.CopySerializedManagedFieldsOnly(TwoDDefaults.instance.Tiles, this);
		}
	}
	
	[ScriptedImporter(0, "tiles")]
	public class TilesImporter : ScriptedImporter
	{
		[SerializeField] private TilesImporterSettings m_settings;
		
		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (m_settings == null) {
				m_settings = new TilesImporterSettings();
				m_settings.Initialize();
			}

			using var fileStream = File.OpenRead(ctx.assetPath);
			using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);

			var texture = ZipArchiveUtils.GetTexture(zipArchive, m_settings);
			ctx.AddObjectToAsset("texture", texture, texture);
			
			var asepriteData = ZipArchiveUtils.GetAsepriteData(zipArchive);
			var sprites = GenerateSprites(texture, asepriteData, m_settings);
			
			foreach (var sprite in sprites) {
				ctx.AddObjectToAsset(sprite.name, sprite);
			}

			if (m_settings.GenerateTiles) {
				var tileCollection = GenerateTiles(asepriteData, sprites);
				tileCollection.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
			
				foreach (var tile in tileCollection.Tiles) {
					ctx.AddObjectToAsset(tile.name, tile);
				}
			
				ctx.AddObjectToAsset("main", tileCollection);
				ctx.SetMainObject(tileCollection);
			}
		}

		private static Sprite[] GenerateSprites(Texture2D texture, AsepriteData data, TilesImporterSettings settings)
		{
			return data.meta.slices.Select(slice => {
				
				var key = slice.keys[0];

				var rect = key.bounds.ToRect();
				rect.y = data.meta.size.h - rect.y - rect.height;
				
				var pivot = slice.name.Contains("tile") ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0);
				
				if (key.IsPivotValid()) {
					pivot.x = key.pivot.x / rect.width;
					pivot.y = 1f - key.pivot.y / rect.height;
				}
				
				var sprite = Sprite.Create(texture, rect, pivot, settings.PixelsPerUnit, 0, SpriteMeshType.FullRect, key.CreateBorder());
				sprite.name = slice.name;
				
				return sprite;
			}).ToArray();
		}
		
		private static TileCollection GenerateTiles(AsepriteData data, Sprite[] sprites)
		{
			var tileCollection = ScriptableObject.CreateInstance<TileCollection>();
			var spriteMap = sprites.ToDictionary(x => x.name);
			
			var tiles = new List<TileBase>();
			foreach (var slice in data.meta.slices) {
				if (!slice.name.Contains("tile")) {
					continue;
				}
				
				if (spriteMap.TryGetValue(slice.name, out var sprite)) {
					var tile = ScriptableObject.CreateInstance<Tile>();
					tile.sprite = sprite;
					tile.name = slice.name;
					
					tiles.Add(tile);
				}
			}

			tileCollection.Tiles = tiles.ToArray();

			return tileCollection;
		}
	}
}