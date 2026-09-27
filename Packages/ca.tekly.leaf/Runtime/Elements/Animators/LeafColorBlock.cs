using System;
using UnityEngine;

namespace Tekly.Leaf.Elements.Animators
{
	[Serializable]
	public struct LeafColorBlock
	{
		public static readonly LeafColorBlock Default = new(Color.white, new Color32(245, 245, 245, 255), new Color32(200, 200, 200, 255));
		
		public Color Normal;
		public Color Highlighted;
		public Color Pressed;

		public LeafColorBlock(Color normal, Color highlighted, Color pressed)
		{
			Normal = normal;
			Highlighted = highlighted;
			Pressed = pressed;
		}

		public readonly Color GetColor(LeafElementMode mode)
		{
			return mode switch {
				LeafElementMode.Normal => Normal,
				LeafElementMode.Highlighted => Highlighted,
				LeafElementMode.Pressed => Pressed,
				_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
			};
		}
	}
}
