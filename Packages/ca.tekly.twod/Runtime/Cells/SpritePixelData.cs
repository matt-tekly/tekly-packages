using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.TwoD.Cells
{
	[Serializable]
	public struct PixelDataSettings
	{
		public bool Generate;
		public float AlphaFilter;
		public float ValueFilter;
	}
	
	[Serializable]
	public struct SpritePixel
	{
		public Vector2 Position;
		public Color Color;
	}
	
	[Serializable]
	public class SpritePixelData
	{
		public List<SpritePixel> Pixels;
		
		public int Width;
		public int Height;

		public SpritePixelData(Texture2D texture, PixelDataSettings pixelDataSettings)
		{
			Width = texture.width;
			Height = texture.height;
			
			Pixels = new List<SpritePixel>((int) ((Width + Height) * 0.33f));
			
			ProcessColors(texture.GetPixels(), pixelDataSettings);
		}

		public SpritePixelData(Sprite sprite, PixelDataSettings pixelDataSettings)
		{
			var rect = sprite.rect;

			Width = (int) rect.width;
			Height = (int) rect.height;

			ProcessColors(sprite.texture.GetPixels((int) rect.x, (int) rect.y, Width, Height), pixelDataSettings);
		}

		private void ProcessColors(Color[] colors, PixelDataSettings pixelDataSettings)
		{
			for (var y = 0; y < Height; y++) {
				for (var x = 0; x < Width; x++) {
					var color = colors[x + y * Width];
					Color.RGBToHSV(color, out var h, out var s, out var v);
					
					if (color.a <= pixelDataSettings.AlphaFilter || v < pixelDataSettings.ValueFilter) {
						continue;
					}

					Pixels.Add(new SpritePixel {
						Position = new Vector3(x, y, 0),
						Color = color
					}); 
				}
			}
		}
	}
}