using System;
using UnityEngine;

namespace Tekly.Common.Ui.Fancy
{
	public enum CornerType
	{
		Round = 0,
		Chamfer = 1,
		Squircle = 2,
		Inverted = 3
	}

	public enum RadiusMode
	{
		Uniform,
		PerCorner,
		Pill
	}

	public enum GradientType
	{
		None = 0,
		Linear = 1,
		Radial = 2,
		Angular = 3,
		Edge = 4
	}

	/// <summary>
	/// How the whole element combines with what is already drawn behind it (other UI and the scene).
	/// This is GPU blend state, so it applies per element, and each mode in use is its own batch.
	/// </summary>
	public enum ShapeBlendMode
	{
		Normal = 0,
		Additive = 1,
		Multiply = 2,
		Screen = 3,
		Darken = 4,
		Lighten = 5,
		Subtract = 6
	}

	/// <summary>
	/// How the sprite is placed in the shape. Values are fixed for serialization; declaration order is the
	/// inspector order. Stretch and Cover work with atlased sprites.
	/// </summary>
	public enum ShapeTextureMode
	{
		/// <summary>Fills the shape's bounds exactly, ignoring the sprite's aspect ratio.</summary>
		Stretch = 0,

		/// <summary>Keeps the sprite's aspect ratio and covers the whole shape; the overflow is cropped.</summary>
		Cover = 2,

		/// <summary>Repeats at the sprite's own size. Needs a whole, non-atlased texture with Wrap Mode = Repeat.</summary>
		Tile = 1
	}

	/// <summary>How the Reveal setting cuts the element (like Image's Filled type, but anti-aliased).</summary>
	public enum RevealMethod
	{
		None = 0,
		Horizontal = 1,
		Vertical = 2,
		Radial = 3
	}

	/// <summary>
	/// Where a reveal starts. Horizontal uses Left/Right, vertical uses Bottom/Top, radial uses all four.
	/// Values are what the shader decodes; the inspector only offers the ones that apply.
	/// </summary>
	public enum RevealOrigin
	{
		Top = 0,
		Right = 1,
		Bottom = 2,
		Left = 3
	}

	public enum OutlineAlignment
	{
		Inside,
		Center,
		Outside
	}

	[Serializable]
	public struct CornerTypes
	{
		public CornerType TopLeft;
		public CornerType TopRight;
		public CornerType BottomRight;
		public CornerType BottomLeft;

		public CornerTypes(CornerType all)
		{
			TopLeft = all;
			TopRight = all;
			BottomRight = all;
			BottomLeft = all;
		}
	}

	/// <summary>
	/// How a layer is colored: a solid color, or a two-color gradient.
	/// </summary>
	[Serializable]
	public struct ShapePaint
	{
		public Color Color;
		public GradientType Gradient;
		public Color Color2;

		[Range(0, 360)]
		public float Angle;

		// Gradient shaping. Stored so that all-zero (the default for existing data) means a plain full gradient.

		[Tooltip("Where the gradient starts (0-1). Before this it is solid Color.")]
		[Range(0, 1)]
		public float RangeStart;

		[Tooltip("How far before the far edge the gradient ends (0-1). After that it is solid Color 2.")]
		[Range(0, 1)]
		public float RangeEndInset;

		[Tooltip("Pushes the halfway point toward Color (negative) or Color 2 (positive).")]
		[Range(-1, 1)]
		public float Bias;

		/// <summary>Where the gradient ends (0-1); 1 - RangeEndInset.</summary>
		public float RangeEnd => 1f - RangeEndInset;

		/// <summary>Position (0-1 within the range) where the colors are mixed 50/50.</summary>
		public float Midpoint => 0.5f - Mathf.Clamp(Bias, -1f, 1f) * 0.4f;

		public static ShapePaint Solid(Color color)
		{
			return new ShapePaint {
				Color = color,
				Gradient = GradientType.None,
				Color2 = color,
				Angle = 90
			};
		}

		public static ShapePaint TwoColor(Color color, Color color2, GradientType gradient, float angle = 90)
		{
			return new ShapePaint {
				Color = color,
				Gradient = gradient,
				Color2 = color2,
				Angle = angle
			};
		}
	}

	[Serializable]
	public struct ShapeOutline
	{
		public bool Enabled;
		public float Width;

		[Tooltip("Extra distance from the shape edge, in the direction of the alignment.")]
		public float Offset;

		public OutlineAlignment Alignment;
		public ShapePaint Paint;

		[Tooltip("Multiply this outline by the element's sprite.")]
		public bool UseTexture;

		public static ShapeOutline Default => new ShapeOutline {
			Enabled = true,
			Width = 4,
			Offset = 0,
			Alignment = OutlineAlignment.Inside,
			Paint = ShapePaint.Solid(Color.white)
		};

		/// <summary>
		/// Band of signed distance (negative inside the shape) that this outline covers.
		/// </summary>
		public Vector2 GetBand()
		{
			var width = Mathf.Max(0, Width);

			return Alignment switch {
				OutlineAlignment.Inside => new Vector2(-Offset - width, -Offset),
				OutlineAlignment.Center => new Vector2(Offset - width * 0.5f, Offset + width * 0.5f),
				_ => new Vector2(Offset, Offset + width)
			};
		}
	}

	[Serializable]
	public struct ShapeShadow
	{
		public bool Enabled;

		[Tooltip("Draw inside the shape (inner shadow) instead of behind it.")]
		public bool Inset;

		public Vector2 Offset;

		[Tooltip("Pull in the shadow's edges that face away from the Offset, so its softness doesn't show past the shape on those sides. Drop shadows only.")]
		public bool Tuck;

		[Tooltip("Grows (outer) or pushes inward (inset) the shadow shape before softening.")]
		public float Spread;

		public float Softness;
		public ShapePaint Paint;

		[Tooltip("Multiply this shadow by the element's sprite.")]
		public bool UseTexture;

		public static ShapeShadow DefaultDrop => new ShapeShadow {
			Enabled = true,
			Inset = false,
			Offset = new Vector2(0, -6),
			Tuck = true,
			Spread = 0,
			Softness = 10,
			Paint = ShapePaint.Solid(new Color(0, 0, 0, 0.35f))
		};

		public static ShapeShadow DefaultInset => new ShapeShadow {
			Enabled = true,
			Inset = true,
			Offset = new Vector2(0, -4),
			Spread = 0,
			Softness = 6,
			Paint = ShapePaint.Solid(new Color(0, 0, 0, 0.3f))
		};
	}

	/// <summary>
	/// A lit edge just inside the shape: brighter where the edge faces the light, darker where it faces away.
	/// Follows corners, bulges and chamfers automatically.
	/// </summary>
	[Serializable]
	public struct ShapeBevel
	{
		public bool Enabled;

		[Tooltip("How far into the shape the bevel reaches.")]
		public float Width;

		[Tooltip("Blur of the bevel's inner edge. 0 gives a crisp band.")]
		public float Softness;

		[Tooltip("Direction the light comes from, in degrees. 90 is straight above, 135 is top-left.")]
		[Range(0, 360)]
		public float LightAngle;

		public Color Highlight;
		public Color Shadow;

		public static ShapeBevel Default => new ShapeBevel {
			Enabled = true,
			Width = 6,
			Softness = 4,
			LightAngle = 135,
			Highlight = new Color(1, 1, 1, 0.5f),
			Shadow = new Color(0, 0, 0, 0.3f)
		};
	}

	/// <summary>
	/// A glossy highlight across the top of the shape: an inset copy of the shape that fades out downward.
	/// </summary>
	[Serializable]
	public struct ShapeGloss
	{
		public bool Enabled;
		public Color Color;

		[Tooltip("Gap between the gloss and the shape's top and side edges.")]
		public float Inset;

		[Tooltip("How far down the gloss reaches, as a fraction of the shape's (inset) height.")]
		[Range(0, 1)]
		public float Height;

		[Tooltip("How much the gloss fades toward its bottom edge. 0 is solid, 1 fades fully to transparent.")]
		[Range(0, 1)]
		public float Fade;

		[Tooltip("Blur of the gloss edges.")]
		public float Softness;

		public static ShapeGloss Default => new ShapeGloss {
			Enabled = true,
			Color = new Color(1, 1, 1, 0.45f),
			Inset = 4,
			Height = 0.5f,
			Fade = 1,
			Softness = 1
		};
	}
}
