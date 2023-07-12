using UnityEditor;
using UnityEngine;

namespace Tekly.TwoD.Common
{
	public interface ITextureSettings
	{
		public TextureFormat TextureFormat { get; }
		public TextureCompressionQuality CompressionQuality { get; }
		public FilterMode FilterMode { get; }
		public int PixelsPerUnit { get; }
	}
}