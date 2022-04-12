//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Webster.Routes.AssetInfos
{
	[Serializable]
	public class AssetsSummary
	{
		public AssetSummary[] AudioClips;
		public AssetSummary[] Materials;
		public AssetSummary[] Meshes;
		public AssetSummary[] Shaders;
		public AssetSummary[] Textures;
		public AssetSummary[] Sprites;

		public AssetsSummary()
		{
			AudioClips = GetSummary<AudioClip>();
			Materials = GetSummary<Material>();
			Meshes = GetSummary<Mesh>();
			Textures = GetSummary<Texture>();
			Shaders = GetSummary<Shader>();
			Sprites = GetSummary<Sprite>();
		}

		private static AssetSummary[] GetSummary<T>() where T : Object
		{
			return Resources.FindObjectsOfTypeAll<T>()
				.Select(x => new AssetSummary(x))
				.ToArray();
		}
	}

	[Serializable]
	public class AssetSummary
	{
		public string Type;
		public string Name;

		public int Width;
		public int Height;
		public string TextureFormat;

		public bool InAtlas;
		public int TextureInstanceId;

		public float Length;

		public string Shader;

		public int Triangles;
		public int SubMeshCount;
		public int VertexCount;
		
		public int InstanceId;

		public AssetSummary(Object obj)
		{
			Type = obj.GetType().Name;
			Name = obj.name;
			InstanceId = obj.GetInstanceID();

			switch (obj) {
				case AudioClip audioClip: {
					var clip = audioClip;
					Length = clip.length;
					break;
				}
				case Material material: {
					Shader = material.shader.name;
					break;
				}
				case Mesh mesh: {
					VertexCount = mesh.vertexCount;
					SubMeshCount = mesh.subMeshCount;
					if (mesh.isReadable) {
                        for (var i = 0; i < SubMeshCount; i++) {
                            Triangles += mesh.GetTriangles(i).Length / 3;
                        }
					}
					break;
				}
				case Texture texture: {
					Width = texture.width;
					Height = texture.height;
					break;
				}
				case Sprite sprite: {
					InAtlas = sprite.packed;
					var textureRect = sprite.textureRect;
					
					Width = Mathf.RoundToInt(textureRect.width);
					Height = Mathf.RoundToInt(textureRect.height);

					TextureInstanceId = sprite.texture.GetInstanceID();
					
					break;
				}
			}
		}
	}
}