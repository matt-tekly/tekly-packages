using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tekly.TwoD.Common
{
	public static class ZipArchiveUtils
	{
		public static Texture2D GetTexture(ZipArchive archive, ITextureSettings settings)
		{
			var entry = archive.Entries.First(x => x.FullName.Contains(".png"));
			using var entryStream = entry.Open();
			using var s = new MemoryStream();

			entryStream.CopyTo(s);
			var textureBytes = s.ToArray();

			var texture = new Texture2D(1, 1, settings.TextureFormat, false);
			texture.LoadImage(textureBytes);

			EditorUtility.CompressTexture(texture, settings.TextureFormat, settings.CompressionQuality);
			
			texture.filterMode = settings.FilterMode;
			texture.alphaIsTransparency = true;
			texture.name = entry.Name.Replace(".png", "");
			
			return texture;
		}
		
		public static AsepriteData GetAsepriteData(ZipArchive zipArchive)
		{
			var jsonEntry = zipArchive.Entries.First(x => x.FullName.Contains(".json"));
			
			using var entryStream = jsonEntry.Open();
			using var reader = new StreamReader(entryStream);
			var json = reader.ReadToEnd();
			
			return JsonUtility.FromJson<AsepriteData>(json);
		}
	}
}