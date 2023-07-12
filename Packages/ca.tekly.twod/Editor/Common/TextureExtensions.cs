using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tekly.TwoD.Common
{
	public static class TextureExtensions
	{
		public static Texture2D GetSpriteTexture(Sprite sprite)
		{
			var spriteTexture = sprite.texture;
			
			if (!sprite.texture.isReadable) {
				spriteTexture = LoadTexture(AssetDatabase.GetAssetPath(sprite.texture));	
			}
			
			var croppedTexture = new Texture2D((int) sprite.rect.width, (int) sprite.rect.height);

			var pixels = spriteTexture.GetPixels((int) sprite.rect.x,
				(int) sprite.rect.y,
				(int) sprite.rect.width,
				(int) sprite.rect.height);

			croppedTexture.SetPixels(pixels);
			croppedTexture.Apply();

			if (!sprite.texture.isReadable) {
				Object.DestroyImmediate(spriteTexture);	
			}
			
			return croppedTexture;
		}

		public static Texture2D LoadTexture(string filePath)
		{
			var bytes = File.ReadAllBytes(filePath);
			var texture = new Texture2D(1, 1);
			texture.LoadImage(bytes);

			return texture;
		}
	}
}

