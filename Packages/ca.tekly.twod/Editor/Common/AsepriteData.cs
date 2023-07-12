using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

namespace Tekly.TwoD.Common
{
	[Serializable]
	public class AseRect
	{
		public float x;
		public float y;
		public float w;
		public float h;

		public Rect ToRect()
		{
			return new Rect(x, y, w, h);
		}

		public bool IsValid()
		{
			return w != 0 && h != 0;
		}
	}

	[Serializable]
	public class AseFrame
	{
		public string filename;
		public AseRect frame;
		public int duration;
	}
	
	[Serializable]
	public class AsepriteData
	{
		public AseFrame[] frames;
		public AseMetaData meta;
	}

	[Serializable]
	public class AseFrameTags
	{
		public string name;
		public int from;
		public int to;
		public string direction;
	}
	
	[Serializable]
	public class AseMetaData
	{
		public AseRect size;
		public AseFrameTags[] frameTags;
		public AseLayer[] layers;
		public AseSlice[] slices;
	}

	[Serializable]
	public class AseLayer
	{
		public string name;
		public AseLayerCell[] cels;
	}

	[Serializable]
	public class AseLayerCell
	{
		public int frame;
		public string data;
	}
	
	[Serializable]
	public class AseSlice
	{
		public string name;
		public AseSliceKey[] keys;
	}

	[Serializable]
	public class AseSliceKey
	{
		public int frame;
		public AseRect bounds;
		public AseRect center;
		
		// This can support having a pivot defined
		// At the moment Aseprite just doesn't include it in the JSON
		// Using the Unity JSON Serializer it is impossible to determine if it wasn't set or if it was set to (0,0)

		public Vector4 CreateBorder()
		{
			if (!bounds.IsValid()) {
				return Vector4.zero;
			}
			var border = new Vector4();
			border.x = center.x; // Left
			border.y = bounds.h - (center.y + center.h); // Bottom
			border.z = bounds.w - (center.x + center.w); // right
			border.w = center.y; // Top
			
			return border;
		}
	}
}