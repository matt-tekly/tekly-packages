using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tekly.TwoD.Cells
{
	public static class CellTextureImporter
	{
		public static Texture2D CreateTexture(ZipArchiveEntry entry, CellImporterSettings settings)
		{
			using var entryStream = entry.Open();
			using var s = new MemoryStream();

			entryStream.CopyTo(s);
			var textureBytes = s.ToArray();

			var sourceTexture = new Texture2D(1, 1, settings.TextureFormat, false);
			sourceTexture.LoadImage(textureBytes);

			EditorUtility.CompressTexture(sourceTexture, settings.TextureFormat, settings.TextureCompressionQuality);

			sourceTexture.filterMode = settings.FilterMode;
			sourceTexture.alphaIsTransparency = true;
			sourceTexture.name = entry.Name.Replace(".png", "");
			
			return sourceTexture;
		}

		public static Sprite[] GenerateSprites(Texture2D texture, AseSpriteData data, CellImporterSettings settings)
		{
			var slices = GatherSlices(data);

			return data.frames.Select((x, index) => {

				var border = Vector4.zero;
				
				if (slices.TryGetValue(index, out var sliceKey)) {
					border = sliceKey.CreateBorder();
				}

				var sprite = Sprite.Create(texture, x.frame.ToRect(), settings.Pivot, settings.PixelsPerUnit, 0, SpriteMeshType.FullRect, border);

				sprite.name = x.filename;
				return sprite;
			}).ToArray();
		}
		
		private static Dictionary<int, AseSliceKey> GatherSlices(AseSpriteData data)
		{
			if (data.meta.slices == null || data.meta.slices.Length == 0) {
				return new Dictionary<int, AseSliceKey>();
			}

			return data.meta.slices.SelectMany(x => x.keys).ToDictionary(x => x.frame);
		}
	}
}