using System;
using UnityEngine;

namespace Tekly.Common.Ui.Styled
{
	public enum StyledRadiusMode
	{
		Uniform,
		PerCorner,
		Pill
	}

	/// <summary>
	/// Which corners of a rect. Used to square off corners that touch a neighbour, like the inner corners of a
	/// segmented button row.
	/// </summary>
	[Flags]
	public enum StyledCorners
	{
		None = 0,
		TopLeft = 1 << 0,
		TopRight = 1 << 1,
		BottomRight = 1 << 2,
		BottomLeft = 1 << 3,
		Top = TopLeft | TopRight,
		Bottom = BottomLeft | BottomRight,
		Left = TopLeft | BottomLeft,
		Right = TopRight | BottomRight,
		All = TopLeft | TopRight | BottomRight | BottomLeft
	}

	/// <summary>
	/// How the whole rect combines with what is already drawn behind it (other UI and the scene). This is GPU
	/// blend state, so it applies to the rect and not to one layer, and each mode in use is its own batch.
	/// Transparent pixels always leave what is behind them unchanged.
	/// </summary>
	public enum StyledBlendMode
	{
		Normal = 0,

		/// <summary>Adds to what is behind: glows and light.</summary>
		Additive = 1,

		/// <summary>Darkens what is behind by the rect's color: tints and shading.</summary>
		Multiply = 2,

		/// <summary>Lightens what is behind without blowing out: soft highlights.</summary>
		Screen = 3
	}

	/// <summary>
	/// Shows only part of the rect, for progress bars and cooldowns. A linear reveal with a Shape cap makes
	/// the shape itself smaller, so a stroke runs around the growing end and a shadow is the shadow of what
	/// shows. A straight cap and a radial reveal cut the finished layers like a mask.
	/// </summary>
	public enum StyledReveal
	{
		None = 0,
		FromLeft = 1,
		FromRight = 2,
		FromBottom = 3,
		FromTop = 4,

		/// <summary>Sweeps around the center, like a cooldown.</summary>
		Radial = 5
	}

	/// <summary>How the growing end of a linear reveal is shaped.</summary>
	public enum StyledRevealCap
	{
		/// <summary>
		/// The end has the rect's own shape, as if the whole rect were sliding into view. A rounded bar keeps
		/// a rounded end at any amount.
		/// </summary>
		Shape = 0,

		/// <summary>The end is cut straight across, like a mask. A stroke stops at the cut and does not run along it.</summary>
		Straight = 1
	}

	/// <summary>Where a radial reveal starts.</summary>
	public enum StyledRadialStart
	{
		Top = 0,
		Right = 1,
		Bottom = 2,
		Left = 3
	}

	/// <summary>How the rect's sprite is placed in the shape.</summary>
	public enum StyledTextureMode
	{
		/// <summary>
		/// Keeps the sprite's aspect ratio and covers the whole shape. The overflow is cropped. For photos,
		/// portraits and item art. Works with atlased sprites.
		/// </summary>
		Cover = 0,

		/// <summary>
		/// Repeats the sprite at its own size, and can scroll. For patterns. Needs a sprite that uses its whole,
		/// non-atlased texture with Wrap Mode set to Repeat.
		/// </summary>
		Tile = 1
	}

	/// <summary>What a layer draws. Values are what the shader reads.</summary>
	public enum StyledLayerKind
	{
		Fill = 0,
		Stroke = 1,

		/// <summary>A blurred copy of the shape behind it, as in CSS box-shadow.</summary>
		Shadow = 2,

		/// <summary>A shadow cast onto the inside of the shape, as in CSS box-shadow with inset.</summary>
		InnerShadow = 3,

		/// <summary>
		/// A lit edge just inside the shape: the paint's Color where the edge faces the light, its Color 2 where
		/// it faces away. The paint's Angle is where the light comes from.
		/// </summary>
		Bevel = 4
	}

	public enum StyledStrokeAlign
	{
		Inside,
		Center,
		Outside
	}

	/// <summary>Values are what the shader reads.</summary>
	public enum StyledGradient
	{
		None = 0,

		/// <summary>Runs across the shape toward the angle, from Color to Color 2.</summary>
		Linear = 1,

		/// <summary>Color at the center, reaching Color 2 at the corners.</summary>
		Radial = 2,

		/// <summary>Follows the shape: Color at the layer's outer edge, Color 2 at its inner edge or deepest point.</summary>
		Edge = 3
	}

	/// <summary>How a layer is colored: a solid color, or a two-color gradient.</summary>
	[Serializable]
	public struct StyledPaint
	{
		public Color Color;
		public StyledGradient Gradient;
		public Color Color2;

		[Tooltip("As in CSS: 0 points up and the angle turns clockwise. The gradient runs toward it, from Color to Color 2.")]
		[Range(0, 360)]
		public float Angle;

		[Tooltip("Where the gradient starts (0-1). Before this it is solid Color.")]
		[Range(0, 1)]
		public float RangeStart;

		// Stored as an inset so that a paint created without setting it still has a full gradient.
		[Tooltip("How far before the far end the gradient stops (0-1). After that it is solid Color 2.")]
		[Range(0, 1)]
		public float RangeEndInset;

		[Tooltip("Bends the gradient: pushes the halfway point toward Color (negative) or toward Color 2 (positive). The same idea as a color hint in CSS.")]
		[Range(-1, 1)]
		public float Bias;

		/// <summary>Where the gradient ends (0-1). After this it is solid Color 2.</summary>
		public float RangeEnd {
			get => 1f - RangeEndInset;
			set => RangeEndInset = 1f - value;
		}

		/// <summary>
		/// Position in the gradient (0-1) where the two colors are mixed half and half: 0.5 with no bias,
		/// from 0.9 at a bias of -1 to 0.1 at a bias of 1.
		/// </summary>
		public float Midpoint => 0.5f - Mathf.Clamp(Bias, -1f, 1f) * 0.4f;

		/// <summary>
		/// The power the gradient position is raised to so that the halfway mix lands on the Midpoint.
		/// 1 with no bias. This is how CSS bends a gradient around a color hint.
		/// </summary>
		public float BiasExponent => Bias == 0f ? 1f : Mathf.Log(0.5f) / Mathf.Log(Midpoint);

		/// <summary>Whether the paint hides everything under it.</summary>
		public bool IsOpaque => Color.a >= 1f && (Gradient == StyledGradient.None || Color2.a >= 1f);

		public static StyledPaint Solid(Color color)
		{
			return new StyledPaint {
				Color = color,
				Gradient = StyledGradient.None,
				Color2 = color
			};
		}

		/// <param name="angle">As in CSS: 0 points up (Color at the bottom, Color 2 at the top) and it turns clockwise.</param>
		public static StyledPaint Linear(Color color, Color color2, float angle = 0)
		{
			return new StyledPaint {
				Color = color,
				Gradient = StyledGradient.Linear,
				Color2 = color2,
				Angle = angle
			};
		}

		/// <summary>Color at the center, Color 2 at the corners.</summary>
		public static StyledPaint Radial(Color color, Color color2)
		{
			return new StyledPaint {
				Color = color,
				Gradient = StyledGradient.Radial,
				Color2 = color2
			};
		}

		/// <summary>Color at the layer's outer edge, Color 2 at its inner edge or deepest point.</summary>
		public static StyledPaint Edge(Color color, Color color2)
		{
			return new StyledPaint {
				Color = color,
				Gradient = StyledGradient.Edge,
				Color2 = color2
			};
		}

		public static implicit operator StyledPaint(Color color)
		{
			return Solid(color);
		}
	}

	/// <summary>
	/// What only a stroke layer uses: where it sits against the shape's edge, its dashes and its own wobble.
	/// </summary>
	[Serializable]
	public struct StyledStrokeStyle
	{
		[Tooltip("Which side of the shape's edge the stroke sits on.")]
		public StyledStrokeAlign Align;

		[Tooltip("Distance from the start of one dash to the next. 0 draws a solid stroke. Adjusted slightly so the dashes meet evenly around the edge.")]
		public float DashSpacing;

		[Tooltip("How much of each step is dash. 0 with Round Caps gives dots.")]
		[Range(0, 1)]
		public float DashLength;

		[Tooltip("Moves the dashes along the edge, in units per second. Clockwise when positive. Uses game time.")]
		public float DashSpeed;

		[Tooltip("How much the stroke's width varies along the edge, for a hand-drawn look.")]
		public float WobbleWidth;

		[Tooltip("How far the stroke drifts in and out from its position along the edge.")]
		public float WobbleDrift;

		[Tooltip("Distance between this stroke's wobbles along the edge. 0 uses the shape's Lobe Length. Adjusted slightly so the wobble loops evenly.")]
		public float WobbleLength;

		[Tooltip("Picks a different wobble pattern for this stroke, so two strokes do not move together.")]
		public float WobbleSeed;

		[Tooltip("Round the ends of each dash. The caps fit inside the dash length.")]
		public bool DashRoundCaps;

		/// <summary>How far the stroke's own wobble can push it past its band.</summary>
		public float WobbleReach => Mathf.Max(0, WobbleWidth) + Mathf.Max(0, WobbleDrift);

		/// <summary>Whether the stroke is dashed or wobbles, which needs the position along the shape's edge.</summary>
		public bool UsesEdgePosition => DashSpacing > 0 || WobbleWidth > 0 || WobbleDrift > 0;
	}

	/// <summary>
	/// One layer of a StyledRect. A rect draws its layers as an ordered list where the first is in front.
	/// Every kind shares this one struct, and fields a kind does not use are ignored. What only one kind
	/// uses is named for it or grouped: a stroke's StrokeStyle, a shadow's ShadowOffset. EdgeOffset is shared: it is
	/// a fill's inset, a stroke's offset and a shadow's spread.
	/// </summary>
	[Serializable]
	public struct StyledLayer
	{
		/// <summary>Band start of a layer with no inner edge, such as a fill. The shader checks for it.</summary>
		public const float NO_INNER_EDGE = -1e9f;

		public bool Enabled;
		public StyledLayerKind Kind;
		public StyledPaint Paint;

		[Tooltip("Stroke and bevel. How wide the stroke is, or how far into the shape the bevel reaches.")]
		public float Width;

		[Tooltip("Moves the layer's edge. Fill: how far it is pulled in from the shape's edge. Stroke: extra distance from the shape's edge, in the direction of the alignment. Shadow: how much it grows before blurring, or how much further in an inner shadow is pushed. Not used by bevels.")]
		public float EdgeOffset;

		[Tooltip("Stroke only. Where the stroke sits, its dashes and its wobble.")]
		public StyledStrokeStyle StrokeStyle;

		[Tooltip("Shadows only. How far the shadow is moved from the shape.")]
		public Vector2 ShadowOffset;

		[Tooltip("Everything but strokes. How far the layer's edge is blurred: a shadow's or fill's edge, or a bevel's inner edge. The same value as a blur in CSS or a design tool.")]
		public float Blur;

		[Tooltip("Shadow only. Also draw the shadow under the shape, where a see-through fill would show it. Off matches CSS.")]
		public bool ShowBehind;

		[Tooltip("Multiply this layer by the rect's sprite. Not used by bevels.")]
		public bool Textured;

		[Tooltip("Draw this layer whole even when the rect is only partly revealed, like the track behind a progress bar.")]
		public bool IgnoreReveal;

		[Tooltip("Keep this layer's own colors when the rect is tinted, by its Tint or by anything that colors the whole graphic such as a Button's color transition. The layer still fades with the tint's alpha.")]
		public bool IgnoreTint;

		public bool IsShadow => Kind == StyledLayerKind.Shadow || Kind == StyledLayerKind.InnerShadow;

		/// <summary>A shadow's spread, which is its EdgeOffset. 0 for everything else.</summary>
		public float Spread => IsShadow ? EdgeOffset : 0f;

		/// <summary>A fill's inset, which is its EdgeOffset. 0 for everything else.</summary>
		public float Inset => Kind == StyledLayerKind.Fill ? EdgeOffset : 0f;

		/// <summary>How far a stroke's own wobble can push it past its band. 0 for everything else.</summary>
		public float WobbleReach => Kind == StyledLayerKind.Stroke ? StrokeStyle.WobbleReach : 0f;

		/// <summary>
		/// Whether the layer needs the position along the shape's edge, which only the Extended shader works
		/// out: a dashed stroke, or a stroke that wobbles.
		/// </summary>
		public bool UsesEdgePosition => Draws && Kind == StyledLayerKind.Stroke && StrokeStyle.UsesEdgePosition;

		/// <summary>Whether the layer puts anything on screen.</summary>
		public bool Draws => Enabled && (!UsesWidth || Width > 0);

		private bool UsesWidth => Kind == StyledLayerKind.Stroke || Kind == StyledLayerKind.Bevel;

		/// <summary>
		/// Width of the layer's soft edge, which fades like a Gaussian blur: four standard deviations, twice the Blur.
		/// Strokes have none.
		/// </summary>
		public float SoftWidth => Kind == StyledLayerKind.Stroke ? 0f : Mathf.Max(0, Blur) * 2f;

		public static StyledLayer Fill(StyledPaint paint)
		{
			return new StyledLayer {
				Enabled = true,
				Kind = StyledLayerKind.Fill,
				Paint = paint
			};
		}

		public static StyledLayer Stroke(float width, StyledPaint paint, StyledStrokeAlign align = StyledStrokeAlign.Inside)
		{
			return new StyledLayer {
				Enabled = true,
				Kind = StyledLayerKind.Stroke,
				Paint = paint,
				Width = width,
				StrokeStyle = new StyledStrokeStyle { Align = align }
			};
		}

		/// <summary>A copy of this stroke with dashes. A length of 0 with round caps gives dots.</summary>
		public StyledLayer WithDashes(float spacing, float length = 0.55f, bool roundCaps = true, float speed = 0)
		{
			var layer = this;
			layer.StrokeStyle.DashSpacing = spacing;
			layer.StrokeStyle.DashLength = length;
			layer.StrokeStyle.DashRoundCaps = roundCaps;
			layer.StrokeStyle.DashSpeed = speed;
			return layer;
		}

		/// <summary>
		/// A copy of this stroke with a wobble of its own: its width varies and it drifts in and out along
		/// the edge. Give two strokes different seeds and they move independently.
		/// </summary>
		/// <param name="length">Distance between wobbles. 0 uses the shape's Lobe Length.</param>
		public StyledLayer WithWobble(float width, float drift, float length = 0, float seed = 0)
		{
			var layer = this;
			layer.StrokeStyle.WobbleWidth = width;
			layer.StrokeStyle.WobbleDrift = drift;
			layer.StrokeStyle.WobbleLength = length;
			layer.StrokeStyle.WobbleSeed = seed;
			return layer;
		}

		/// <summary>
		/// A glossy highlight across the top of the shape: a fill pulled in from the edge that fades out downward.
		/// It is an ordinary fill, so everything about it can be changed afterwards.
		/// </summary>
		/// <param name="inset">Gap between the gloss and the shape's edge.</param>
		/// <param name="blur">Blur of the gloss's edge.</param>
		/// <param name="height">How far down the gloss reaches, as a fraction of the shape's height.</param>
		public static StyledLayer Gloss(Color color, float inset, float blur, float height = 0.5f)
		{
			var faded = color;
			faded.a = 0f;

			var paint = StyledPaint.Linear(faded, color, 0);
			paint.RangeStart = 1f - Mathf.Clamp01(height);

			return new StyledLayer {
				Enabled = true,
				Kind = StyledLayerKind.Fill,
				Paint = paint,
				EdgeOffset = inset,
				Blur = blur
			};
		}

		/// <param name="offset">As in CSS, but with y up: (0, -6) is a shadow below the shape.</param>
		public static StyledLayer DropShadow(Vector2 offset, float blur, StyledPaint paint, float spread = 0)
		{
			return new StyledLayer {
				Enabled = true,
				Kind = StyledLayerKind.Shadow,
				Paint = paint,
				ShadowOffset = offset,
				Blur = blur,
				EdgeOffset = spread
			};
		}

		/// <param name="offset">With y up: (0, -4) shades the top inside edge.</param>
		public static StyledLayer InnerShadow(Vector2 offset, float blur, StyledPaint paint, float spread = 0)
		{
			return new StyledLayer {
				Enabled = true,
				Kind = StyledLayerKind.InnerShadow,
				Paint = paint,
				ShadowOffset = offset,
				Blur = blur,
				EdgeOffset = spread
			};
		}

		/// <param name="lightAngle">Where the light comes from. As in CSS: 0 is straight above and it turns clockwise, so 315 is top-left.</param>
		public static StyledLayer Bevel(float width, float blur, Color highlight, Color shadow, float lightAngle = 315)
		{
			return new StyledLayer {
				Enabled = true,
				Kind = StyledLayerKind.Bevel,
				Paint = new StyledPaint { Color = highlight, Color2 = shadow, Angle = lightAngle },
				Width = width,
				Blur = blur
			};
		}

		/// <summary>
		/// The band of signed distance (negative inside the shape) that the layer covers: x is its inner edge,
		/// y its outer edge. Only strokes and bevels have an inner edge. A fill's outer edge moves in by its EdgeOffset.
		/// </summary>
		public Vector2 GetBand()
		{
			var width = Mathf.Max(0, Width);

			if (Kind == StyledLayerKind.Bevel) {
				return new Vector2(-width, 0);
			}

			if (Kind == StyledLayerKind.Fill) {
				return new Vector2(NO_INNER_EDGE, -EdgeOffset);
			}

			if (Kind != StyledLayerKind.Stroke) {
				return new Vector2(NO_INNER_EDGE, 0);
			}

			var offset = EdgeOffset;

			return StrokeStyle.Align switch {
				StyledStrokeAlign.Inside => new Vector2(-offset - width, -offset),
				StyledStrokeAlign.Center => new Vector2(offset - width * 0.5f, offset + width * 0.5f),
				_ => new Vector2(offset, offset + width)
			};
		}

		/// <summary>
		/// Whether two versions of a layer cover the same pixels, so that going from one to the other only
		/// needs new values in the data texture and not a new mesh.
		/// </summary>
		public static bool SameGeometry(in StyledLayer a, in StyledLayer b)
		{
			if (a.Draws != b.Draws) {
				return false;
			}

			if (!a.Draws) {
				return true;
			}

			if (a.Kind != b.Kind || a.GetBand() != b.GetBand() || a.WobbleReach != b.WobbleReach) {
				return false;
			}

			// Under a slide reveal a layer that follows the reveal is drawn without a hole.
			if (a.IgnoreReveal != b.IgnoreReveal) {
				return false;
			}

			// A layer with a soft edge reaches as far as its blur.
			if (a.SoftWidth != b.SoftWidth) {
				return false;
			}

			return !a.IsShadow || (a.ShadowOffset == b.ShadowOffset && a.Blur == b.Blur && a.EdgeOffset == b.EdgeOffset && a.ShowBehind == b.ShowBehind);
		}
	}

	/// <summary>
	/// Where a StyledRect's values sit in the data texture. A rect owns one run of texels: its shape, then each
	/// of its layers. Every texel is four floats. The shader include StyledRectCore.cginc reads the same layout,
	/// so change both together.
	///
	/// Shape texels:
	///   0  half size x, half size y, unused, unused
	///   1  corner radii: top-left, top-right, bottom-right, bottom-left (already fitted to the size)
	///   2  corner exponents, one per corner in the order of the radii: 2 is a circular corner, 4 a squircle
	///   3  shape wobble: amount, lobe length, seed, changes per second
	///   4  reveal: mode, then by mode. 0 = none. 1 = slide: the shape's copy is moved by x, y. 2 = straight cut:
	///      direction x, y and position. 3 = radial: start angle and signed sweep in radians. 5 = nothing shows.
	///   5  texture matrix: uv.x = dot(xy, position), uv.y = dot(zw, position)
	///   6  texture offset x, y, then scroll x, y in tiles per second
	///   7  texture uv limits: min x, min y, max x, max y. Keeps a Cover sprite inside its own part of an atlas.
	///
	/// Layer texels:
	///   0  kind plus 8 times the flags (1 = underlay: ends the layer a pixel early, see StyledRect.WriteRows.
	///      2 = show behind: a shadow is not cut out under the shape. 4 = textured. 8 = ignore tint.
	///      16 = ignore reveal),
	///      soft rounding: the least radius the corners have for an inner shadow, a blurred fill or an Edge
	///      gradient, 0 for none (see StyledRect.SoftRounding),
	///      band start, band end
	///   1  offset x, offset y, soft width (twice the blur), spread. Offset and spread are for shadows.
	///   2  color A (gamma, straight alpha)
	///   3  color B (gamma, straight alpha)
	///   4  gradient type, angle in radians, range start, range end
	///   5  gradient exponent (1 leaves the gradient straight, see StyledPaint.BiasExponent), then a stroke's
	///      dashes: spacing (0 = solid), dash length as a fraction of the spacing plus 2 for round caps, speed
	///   6  stroke wobble: width variation, drift, length (0 = the shape's lobe length), seed
	/// </summary>
	public static class StyledRows
	{
		public const int SHAPE_TEXELS = 8;
		public const int SHAPE_SIZE = 0;
		public const int SHAPE_RADII = 1;
		public const int SHAPE_CORNER_EXPONENTS = 2;
		public const int SHAPE_WOBBLE = 3;
		public const int SHAPE_REVEAL = 4;
		public const int SHAPE_TEXTURE_MATRIX = 5;
		public const int SHAPE_TEXTURE_OFFSET = 6;
		public const int SHAPE_TEXTURE_LIMITS = 7;

		public const int LAYER_TEXELS = 7;
		public const int LAYER_HEADER = 0;
		public const int LAYER_SHADOW = 1;
		public const int LAYER_COLOR_A = 2;
		public const int LAYER_COLOR_B = 3;
		public const int LAYER_GRADIENT = 4;
		public const int LAYER_CURVE_DASHES = 5;
		public const int LAYER_STROKE_WOBBLE = 6;

		// The flags share the header's first value with the layer kind, which stays below this.
		public const int FLAG_SCALE = 8;

		public const int FLAG_UNDERLAY = 1;
		public const int FLAG_SHOW_BEHIND = 2;
		public const int FLAG_TEXTURED = 4;
		public const int FLAG_IGNORE_TINT = 8;
		public const int FLAG_IGNORE_REVEAL = 16;

		public const int REVEAL_NONE = 0;
		public const int REVEAL_SLIDE = 1;
		public const int REVEAL_STRAIGHT = 2;
		public const int REVEAL_RADIAL = 3;
		public const int REVEAL_HIDDEN = 5;

		public static int SpanTexels(int layerCount)
		{
			return SHAPE_TEXELS + layerCount * LAYER_TEXELS;
		}

		public static int LayerAddress(int spanAddress, int layerIndex)
		{
			return spanAddress + SHAPE_TEXELS + layerIndex * LAYER_TEXELS;
		}

		/// <summary>
		/// The layer address a vertex carries in a fill's solid middle: negative, so the shader knows the whole
		/// quad is covered. The shader turns it back with -address - 1.
		/// </summary>
		public static int SolidAddress(int layerAddress)
		{
			return -layerAddress - 1;
		}
	}
}
