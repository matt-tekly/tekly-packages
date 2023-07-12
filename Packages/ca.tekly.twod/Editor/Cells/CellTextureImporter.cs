using System.Collections.Generic;
using System.Linq;
using Tekly.TwoD.Common;
using UnityEngine;

namespace Tekly.TwoD.Cells
{
	public static class CellTextureImporter
	{
		public static Sprite[] GenerateSprites(Texture2D texture, AsepriteData data, CellImporterSettings settings)
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
		
		private static Dictionary<int, AseSliceKey> GatherSlices(AsepriteData data)
		{
			if (data.meta.slices == null || data.meta.slices.Length == 0) {
				return new Dictionary<int, AseSliceKey>();
			}

			return data.meta.slices.SelectMany(x => x.keys).ToDictionary(x => x.frame);
		}
	}
}