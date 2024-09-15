using System.Linq;
using UnityEngine;

namespace Tekly.Tinker.Http
{
	public static class TextureUtils
	{
		public static byte[] GetSpriteBytes(int instanceId)
		{
			var sprite = FindResource<Sprite>(instanceId);
			return GetSpriteBytes(sprite);
		}
		
		public static T FindResource<T>(int instanceId) where T : Object
		{
			return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(resource => resource.GetInstanceID() == instanceId);
		}
		
		public static T FindResource<T>(string name) where T : Object
		{
			var resources = Resources.FindObjectsOfTypeAll<T>();
			return resources.FirstOrDefault(resource => resource.name == name);
		}

		public static byte[] GetSpriteBytes(Sprite sprite)
		{
			if (sprite == null) {
				return null;
			}
			
			var readableTexture = GetReadableTexture(sprite.texture);

			var textureRect = sprite.textureRect;
			var croppedTexture = new Texture2D(Mathf.RoundToInt(textureRect.width), Mathf.RoundToInt(textureRect.height));

			var pixels = readableTexture.GetPixels(
				Mathf.RoundToInt(textureRect.x),
				Mathf.RoundToInt(textureRect.y),
				Mathf.RoundToInt(textureRect.width),
				Mathf.RoundToInt(textureRect.height)
			);

			croppedTexture.SetPixels(pixels);
			croppedTexture.Apply();

			Object.DestroyImmediate(readableTexture);

			var textureBytes = croppedTexture.EncodeToPNG();

			Object.DestroyImmediate(croppedTexture);

			return textureBytes;
		}

		public static byte[] GetTextureBytes(int instanceId)
		{
			var sprite = FindResource<Texture>(instanceId);
			return GetTextureBytes(sprite);
		}
		
		public static byte[] GetTextureBytes(string name)
		{
			var sprite = FindResource<Texture>(name);
			return GetTextureBytes(sprite);
		}

		public static byte[] GetTextureBytes(Texture texture)
		{
			if (texture == null) {
				return null;
			}
			
			if (texture.isReadable && texture is Texture2D texture2D) {
				return texture2D.EncodeToPNG();
			}
			
			var readableTexture = GetReadableTexture(texture);
			var textureBytes = readableTexture.EncodeToPNG();

			Object.DestroyImmediate(readableTexture);
			return textureBytes;
		}

		public static Texture2D GetReadableTexture(Texture texture)
		{
			var tmp = RenderTexture.GetTemporary( 
				texture.width,
				texture.height,
				0,
				RenderTextureFormat.Default,
				RenderTextureReadWrite.Linear);

			Graphics.Blit(texture, tmp);
			
			var previous = RenderTexture.active;
			
			RenderTexture.active = tmp;
			
			var myTexture2D = new Texture2D(texture.width, texture.height);
			myTexture2D.name = "TEMP_TEKLY_TEX";
			
			// TODO: SystemInfo.graphicsUVStartsAtTop do we need to do something for this
			// Copy the pixels from the RenderTexture to the new Texture
			myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
			myTexture2D.Apply();
			
			RenderTexture.active = previous;
			
			RenderTexture.ReleaseTemporary(tmp);

			return myTexture2D;
		}
	}
}