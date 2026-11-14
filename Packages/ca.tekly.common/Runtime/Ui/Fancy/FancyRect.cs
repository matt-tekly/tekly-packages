using System.Collections.Generic;
using UnityEngine;
using GpuBlendMode = UnityEngine.Rendering.BlendMode;
using GpuBlendOp = UnityEngine.Rendering.BlendOp;
using UnityEngine.UI;

namespace Tekly.Common.Ui.Fancy
{
	/// <summary>
	/// A resolution-independent UI shape: per-corner radius and corner type, per-edge bulge, a fill,
	/// any number of outlines and drop/inner shadows, each with solid or gradient paint.
	/// Outlines can be styled along the edge with dashes, notches or a hand-drawn wobble.
	///
	/// Every layer is emitted as its own quad and evaluated against one shared signed distance field in
	/// the shader, so the whole element batches with one material. An optional sprite multiplies the fill,
	/// and any outline or shadow that opts in with UseTexture.
	/// Layer order, back to front: outer shadows, background, fill, inner shadows, bevel, gloss, outlines.
	/// </summary>
	[AddComponentMenu("UI/Fancy Rect")]
	[RequireComponent(typeof(CanvasRenderer))] // Like Image: Graphic only adds one lazily, too late for a fresh component.
	public class FancyRect : MaskableGraphic, ICanvasRaycastFilter
	{
		public const AdditionalCanvasShaderChannels NEEDED_SHADER_CHANNELS = AdditionalCanvasShaderChannels.TexCoord1 |
		                                                                     AdditionalCanvasShaderChannels.TexCoord2 |
		                                                                     AdditionalCanvasShaderChannels.TexCoord3 |
		                                                                     AdditionalCanvasShaderChannels.Tangent;

		public const string SHADER_NAME = "UI/Fancy Rect";
		public const int MAX_LAYERS_PER_LIST = 8;

		/// <summary>Largest value the 12-bit, 0.25 px vertex encoding can hold.</summary>
		public const float MAX_DISTANCE = 511f;

		private const float EDGE_PADDING = 4f;
		private const int LAYER_BAND = 0;
		private const int LAYER_INNER_SHADOW = 1;
		private const int LAYER_BEVEL = 2;

		// An outline with a style: a band whose uv2.z carries the packed style instead of an offset.
		private const int LAYER_STYLED_BAND = 3;
		private const int NO_INNER_EDGE_CODE = 0;
		private const int FLAG_TEXTURED = 1 << 13;

		// An opaque layer above covers this layer's outer edge, so the shader ends it a pixel early.
		private const int FLAG_UNDERLAY = 1 << 14;

		// Solid layers cover the whole shape (band from the center to the edge), so their band slot holds scroll.
		private const int FLAG_SOLID = 1 << 23;

		private const int REVEAL_CLOCKWISE_BIT = 1 << 4;
		private const int REVEAL_VALUE_SHIFT = 5;
		private const int REVEAL_OFFSET_SPACE_BIT = 1 << 21;
		// Spare uv3.w bits above the reveal: the layer ignores the CanvasRenderer RGB, and the anchor color is Color 2.
		private const int IGNORE_RENDERER_TINT_BIT = 1 << 22;
		private const int ANCHOR_IS_COLOR2_BIT = 1 << 23;

		// Textured layers sample slightly past the texture's edge so anti-aliased fringes pick up its color.
		private const float TEXTURE_EDGE_MARGIN = 1.5f;

		// Tucked shadow edges stop this far inside the shape edge, so the anti-aliased fringe can't show either.
		private const float TUCK_MARGIN = 1f;

		// Edge-only layers are drawn as a ring around their empty middle once the hole is at least this big on both
		// sides and covers this much of the layer quad. Below that the extra quads cost more than the pixels saved.
		private const float MIN_RING_HOLE_SIZE = 16f;
		private const float MIN_RING_HOLE_FRACTION = 0.25f;

		/// <summary>Largest texture scroll speed the vertex encoding can hold, in texture units per second.</summary>
		public const float MAX_TEXTURE_SCROLL = 7.99f;

		/// <summary>
		/// Largest half size the vertex encoding can hold. A bigger element still fills its rect, but its radii,
		/// outlines and other distances grow in proportion.
		/// </summary>
		public const float MAX_HALF_SIZE = 8176f;

		private const float RAYCAST_TOLERANCE = 0.01f;

		// A soft edge fades like a Gaussian blur whose 4 standard deviations span the softness. Its tails run
		// past that, and three deviations each way is where they stop being visible.
		private const float SOFTNESS_REACH = 0.75f;

		[SerializeField] private RadiusMode m_radiusMode = RadiusMode.Uniform;
		[SerializeField] private float m_radius = 16;
		[SerializeField] private Vector4 m_cornerRadii = new Vector4(16, 16, 16, 16);
		[SerializeField] private CornerType m_cornerType = CornerType.Round;
		[SerializeField] private CornerTypes m_cornerTypes;

		[Tooltip("Corners that use the radius. Others are square. Driven by SegmentCorners on a FlowLayout.")]
		[SerializeField] private RectCorners m_roundedCorners = RectCorners.All;

		[Tooltip("Top, Right, Bottom, Left. Positive bulges outward, negative curves inward.")]
		[SerializeField] private Vector4 m_bulge;

		[Tooltip("Shrink the shape by its outward bulge so the bulged edges stay inside the RectTransform.")]
		[SerializeField] private bool m_containBulge;

		[SerializeField] private Sprite m_sprite;
		[SerializeField] private ShapeTextureMode m_textureMode = ShapeTextureMode.Stretch;
		[SerializeField] private Vector2 m_textureScale = Vector2.one;
		[SerializeField] private Vector2 m_textureOffset;
		[SerializeField, Range(-180, 180)] private float m_textureRotation;

		[Tooltip("Tile mode only. Scroll speed in tiles per second, along the texture's own (rotated) axes.")]
		[SerializeField] private Vector2 m_textureScroll;

		[SerializeField] private bool m_fillEnabled = true;
		[SerializeField] private ShapePaint m_fill = ShapePaint.Solid(Color.white);

		[Tooltip("An untextured layer under the fill, so transparent parts of the texture show a color instead of what's behind.")]
		[SerializeField] private bool m_backgroundEnabled;
		[SerializeField] private ShapePaint m_background = ShapePaint.Solid(new Color(0.2f, 0.2f, 0.2f, 1f));
		[SerializeField] private List<ShapeOutline> m_outlines = new List<ShapeOutline>();
		[SerializeField] private List<ShapeShadow> m_shadows = new List<ShapeShadow>();

		[Tooltip("How the element combines with what is behind it. Ignored when a custom Material is assigned.")]
		[SerializeField] private ShapeBlendMode m_blendMode = ShapeBlendMode.Normal;

		[SerializeField] private ShapeBevel m_bevel;
		[SerializeField] private ShapeGloss m_gloss;

		[Tooltip("Shows only part of the element, like Image's Filled type but with anti-aliased edges.")]
		[SerializeField] private RevealMethod m_revealMethod = RevealMethod.None;
		[SerializeField] private RevealOrigin m_revealOrigin = RevealOrigin.Left;
		[SerializeField, Range(0, 1)] private float m_revealAmount = 1;
		[SerializeField] private bool m_revealClockwise = true;

		[Tooltip("Ignore raycasts outside the shape instead of using the full rect.")]
		[SerializeField] private bool m_raycastUsesShape = true;

		[Tooltip("Layers tinted by the CanvasRenderer color (CrossFadeColor, Selectable color tint, Leaf animators). Others keep their colors but still fade with its alpha.")]
		[SerializeField] private FancyRectLayers m_rendererTintLayers = FancyRectLayers.All;

		private const int BLEND_COLOR_PREMULTIPLIED = 0;
		private const int BLEND_COLOR_TOWARD_WHITE = 1;
		private const int BLEND_ALPHA_COVERAGE = 0;
		private const int BLEND_ALPHA_ZERO = 1;
		private const int BLEND_ALPHA_ONE = 2;

		private static readonly int s_srcBlendId = Shader.PropertyToID("_SrcBlend");
		private static readonly int s_dstBlendId = Shader.PropertyToID("_DstBlend");
		private static readonly int s_blendOpId = Shader.PropertyToID("_BlendOp");
		private static readonly int s_blendColorOutputId = Shader.PropertyToID("_BlendColorOutput");
		private static readonly int s_blendAlphaOutputId = Shader.PropertyToID("_BlendAlphaOutput");

		private static readonly Material[] s_blendMaterials = new Material[7];

		/// <summary>
		/// Shared material for a blend mode, so every element using the same mode can batch together.
		/// </summary>
		public static Material GetBlendMaterial(ShapeBlendMode blendMode)
		{
			var index = Mathf.Clamp((int)blendMode, 0, s_blendMaterials.Length - 1);
			var material = s_blendMaterials[index];

			if (material != null) {
				return material;
			}

			material = new Material(Shader.Find(SHADER_NAME)) {
				name = $"Fancy Rect ({(ShapeBlendMode)index})",
				hideFlags = HideFlags.HideAndDontSave
			};

			ApplyBlendMode(material, (ShapeBlendMode)index);
			s_blendMaterials[index] = material;

			return material;
		}

#if UNITY_EDITOR
		/// <summary>
		/// A domain reload (script recompile, entering Play Mode) clears the static cache but not the native materials,
		/// and HideAndDontSave keeps Unity from unloading them, so without this each reload would orphan them.
		/// Destroying them first is safe: every FancyRect is re-enabled after the reload and rebuilds its material.
		/// </summary>
		[UnityEditor.InitializeOnLoadMethod]
		private static void RegisterMaterialCleanup()
		{
			UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += DestroyBlendMaterials;
		}

		private static void DestroyBlendMaterials()
		{
			for (var i = 0; i < s_blendMaterials.Length; i++) {
				if (s_blendMaterials[i] != null) {
					DestroyImmediate(s_blendMaterials[i]);
					s_blendMaterials[i] = null;
				}
			}
		}
#endif

		/// <summary>
		/// Writes a blend mode's GPU state into a material using the Fancy Rect shader.
		/// Transparent pixels always leave the destination (color and alpha) unchanged.
		/// </summary>
		public static void ApplyBlendMode(Material material, ShapeBlendMode blendMode)
		{
			var (src, dst, op, colorOutput, alphaOutput) = blendMode switch {
				// dst + src * a
				ShapeBlendMode.Additive => (GpuBlendMode.One, GpuBlendMode.One, GpuBlendOp.Add, BLEND_COLOR_PREMULTIPLIED, BLEND_ALPHA_ZERO),
				// dst * lerp(1, src, a)
				ShapeBlendMode.Multiply => (GpuBlendMode.DstColor, GpuBlendMode.Zero, GpuBlendOp.Add, BLEND_COLOR_TOWARD_WHITE, BLEND_ALPHA_ONE),
				// 1 - (1 - dst) * (1 - src * a)
				ShapeBlendMode.Screen => (GpuBlendMode.OneMinusDstColor, GpuBlendMode.One, GpuBlendOp.Add, BLEND_COLOR_PREMULTIPLIED, BLEND_ALPHA_ZERO),
				// min(dst, lerp(1, src, a))
				ShapeBlendMode.Darken => (GpuBlendMode.One, GpuBlendMode.One, GpuBlendOp.Min, BLEND_COLOR_TOWARD_WHITE, BLEND_ALPHA_ONE),
				// max(dst, src * a)
				ShapeBlendMode.Lighten => (GpuBlendMode.One, GpuBlendMode.One, GpuBlendOp.Max, BLEND_COLOR_PREMULTIPLIED, BLEND_ALPHA_ZERO),
				// dst - src * a
				ShapeBlendMode.Subtract => (GpuBlendMode.One, GpuBlendMode.One, GpuBlendOp.ReverseSubtract, BLEND_COLOR_PREMULTIPLIED, BLEND_ALPHA_ZERO),
				// Normal: premultiplied alpha
				_ => (GpuBlendMode.One, GpuBlendMode.OneMinusSrcAlpha, GpuBlendOp.Add, BLEND_COLOR_PREMULTIPLIED, BLEND_ALPHA_COVERAGE)
			};

			material.SetFloat(s_srcBlendId, (float)src);
			material.SetFloat(s_dstBlendId, (float)dst);
			material.SetFloat(s_blendOpId, (float)op);
			material.SetFloat(s_blendColorOutputId, colorOutput);
			material.SetFloat(s_blendAlphaOutputId, alphaOutput);
		}

		public override Material defaultMaterial => GetBlendMaterial(m_blendMode);

		public override Texture mainTexture => m_sprite != null ? m_sprite.texture : s_WhiteTexture;

		/// <summary>Optional sprite that multiplies the fill (and any layer with UseTexture).</summary>
		public Sprite Sprite {
			get => m_sprite;
			set {
				if (m_sprite == value) {
					return;
				}

				m_sprite = value;
				SetAllDirty();
			}
		}

		public ShapeTextureMode TextureMode {
			get => m_textureMode;
			set => SetField(ref m_textureMode, value);
		}

		/// <summary>Stretch: zooms the sprite. Tile: multiplies the tile size.</summary>
		public Vector2 TextureScale {
			get => m_textureScale;
			set => SetField(ref m_textureScale, value);
		}

		/// <summary>Offset in texture space (1 = one full sprite / tile).</summary>
		public Vector2 TextureOffset {
			get => m_textureOffset;
			set => SetField(ref m_textureOffset, value);
		}

		/// <summary>
		/// Tile mode only. Scroll speed in tiles per second along the texture's own axes, animated in the shader
		/// from game time (_Time), so it stops when Time.timeScale is 0.
		/// </summary>
		public Vector2 TextureScroll {
			get => m_textureScroll;
			set => SetField(ref m_textureScroll, value);
		}

		/// <summary>Rotation of the texture in degrees, around the shape center.</summary>
		public float TextureRotation {
			get => m_textureRotation;
			set => SetField(ref m_textureRotation, value);
		}

		public ShapeBlendMode BlendMode {
			get => m_blendMode;
			set {
				if (m_blendMode == value) {
					return;
				}

				m_blendMode = value;
				SetMaterialDirty();
			}
		}

		public RadiusMode RadiusMode {
			get => m_radiusMode;
			set => SetField(ref m_radiusMode, value);
		}

		/// <summary>Radius used by RadiusMode.Uniform.</summary>
		public float Radius {
			get => m_radius;
			set => SetField(ref m_radius, Mathf.Max(0, value));
		}

		/// <summary>Radii used by RadiusMode.PerCorner: top-left, top-right, bottom-right, bottom-left.</summary>
		public Vector4 CornerRadii {
			get => m_cornerRadii;
			set => SetField(ref m_cornerRadii, Vector4.Max(value, Vector4.zero));
		}

		/// <summary>Corner type for Uniform and Pill modes.</summary>
		public CornerType CornerType {
			get => m_cornerType;
			set => SetField(ref m_cornerType, value);
		}

		/// <summary>Corner types for RadiusMode.PerCorner.</summary>
		public CornerTypes CornerTypes {
			get => m_cornerTypes;
			set {
				m_cornerTypes = value;
				SetVerticesDirty();
			}
		}

		/// <summary>
		/// Corners that use the radius; the rest are square. Applies to every radius mode.
		/// </summary>
		public RectCorners RoundedCorners {
			get => m_roundedCorners;
			set => SetField(ref m_roundedCorners, value);
		}

		/// <summary>
		/// When true, the shape is inset by the outward bulge on each side so the bulged edges touch the rect
		/// instead of spilling past it. Outside outlines and shadows still extend beyond the rect.
		/// </summary>
		public bool ContainBulge {
			get => m_containBulge;
			set => SetField(ref m_containBulge, value);
		}

		/// <summary>Top, right, bottom, left. Positive bulges outward, negative curves inward.</summary>
		public Vector4 Bulge {
			get => m_bulge;
			set => SetField(ref m_bulge, value);
		}

		public bool FillEnabled {
			get => m_fillEnabled;
			set => SetField(ref m_fillEnabled, value);
		}

		public ShapePaint Fill {
			get => m_fill;
			set {
				m_fill = value;
				SetVerticesDirty();
			}
		}

		/// <summary>Lit inner edge. Set Enabled to show it.</summary>
		public ShapeBevel Bevel {
			get => m_bevel;
			set {
				m_bevel = value;
				SetVerticesDirty();
			}
		}

		/// <summary>Glossy highlight across the top. Set Enabled to show it.</summary>
		public ShapeGloss Gloss {
			get => m_gloss;
			set {
				m_gloss = value;
				SetVerticesDirty();
			}
		}

		/// <summary>How the element is partially revealed (health bars, timers). None shows all of it.</summary>
		public RevealMethod RevealMethod {
			get => m_revealMethod;
			set => SetField(ref m_revealMethod, value);
		}

		/// <summary>Where the reveal starts: Left/Right for horizontal, Bottom/Top for vertical, any for radial.</summary>
		public RevealOrigin RevealOrigin {
			get => m_revealOrigin;
			set => SetField(ref m_revealOrigin, value);
		}

		/// <summary>0 hides the element, 1 shows all of it.</summary>
		public float RevealAmount {
			get => m_revealAmount;
			set => SetField(ref m_revealAmount, Mathf.Clamp01(value));
		}

		/// <summary>Radial reveals only: sweep clockwise from the origin.</summary>
		public bool RevealClockwise {
			get => m_revealClockwise;
			set => SetField(ref m_revealClockwise, value);
		}

		/// <summary>Draw the untextured background layer under the fill.</summary>
		public bool BackgroundEnabled {
			get => m_backgroundEnabled;
			set => SetField(ref m_backgroundEnabled, value);
		}

		public ShapePaint Background {
			get => m_background;
			set {
				m_background = value;
				SetVerticesDirty();
			}
		}

		public bool RaycastUsesShape {
			get => m_raycastUsesShape;
			set => m_raycastUsesShape = value;
		}

		/// <summary>Layers the CanvasRenderer color tints. Others keep their colors but still fade with its alpha.</summary>
		public FancyRectLayers RendererTintLayers {
			get => m_rendererTintLayers;
			set => SetField(ref m_rendererTintLayers, value);
		}

		/// <summary>
		/// Sets a field and rebuilds the mesh, but only when the value changed. For floats, bools, enums and vectors.
		/// </summary>
		private void SetField<T>(ref T field, T value)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) {
				return;
			}

			field = value;
			SetVerticesDirty();
		}

		public int OutlineCount => m_outlines.Count;
		public int ShadowCount => m_shadows.Count;

		public ShapeOutline GetOutline(int index) => m_outlines[index];
		public ShapeShadow GetShadow(int index) => m_shadows[index];

		public void SetOutline(int index, ShapeOutline outline)
		{
			m_outlines[index] = outline;
			SetVerticesDirty();
		}

		public void AddOutline(ShapeOutline outline)
		{
			if (m_outlines.Count >= MAX_LAYERS_PER_LIST) {
				Debug.LogWarning($"[FancyRect] At most {MAX_LAYERS_PER_LIST} outlines are supported.", this);
				return;
			}

			m_outlines.Add(outline);
			SetVerticesDirty();
		}

		public void RemoveOutline(int index)
		{
			m_outlines.RemoveAt(index);
			SetVerticesDirty();
		}

		public void SetShadow(int index, ShapeShadow shadow)
		{
			m_shadows[index] = shadow;
			SetVerticesDirty();
		}

		public void AddShadow(ShapeShadow shadow)
		{
			if (m_shadows.Count >= MAX_LAYERS_PER_LIST) {
				Debug.LogWarning($"[FancyRect] At most {MAX_LAYERS_PER_LIST} shadows are supported.", this);
				return;
			}

			m_shadows.Add(shadow);
			SetVerticesDirty();
		}

		public void RemoveShadow(int index)
		{
			m_shadows.RemoveAt(index);
			SetVerticesDirty();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			FixTexCoordsInCanvas();
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();
			FixTexCoordsInCanvas();
		}

		protected override void OnCanvasHierarchyChanged()
		{
			base.OnCanvasHierarchyChanged();
			FixTexCoordsInCanvas();
		}

		private void FixTexCoordsInCanvas()
		{
			var nearestCanvas = canvas;

			if (nearestCanvas == null) {
				return;
			}

			EnsureShaderChannels(nearestCanvas);
			EnsureShaderChannels(nearestCanvas.rootCanvas);
		}

		private static void EnsureShaderChannels(Canvas target)
		{
			if (target != null && (target.additionalShaderChannels & NEEDED_SHADER_CHANNELS) != NEEDED_SHADER_CHANNELS) {
				target.additionalShaderChannels |= NEEDED_SHADER_CHANNELS;
			}
		}

		private struct ShapeData
		{
			public Vector2 Center;

			/// <summary>
			/// Half size as the shader sees it: rounded to what the vertex data can encode. The layer's geometry is
			/// built in this space and InvScale maps it back onto the rect, so the edges land exactly on the rect.
			/// </summary>
			public Vector2 HalfSize;
			public Vector2 InvScale;
			public Vector4 Radii;
			public Vector4 Bulge;
			public int CornerBits;
			public Vector4 Uv1;
			public float PackedHalfSize;

			public ShapeTextureMode TextureMode;
			public Vector4 OuterUv;
			public Vector2 TextureCenter;
			public Vector2 TextureDivisor;
			public Vector2 TextureOffset;
			public float TextureCos;
			public float TextureSin;
			public float PackedScroll;

			// Reveal: header bits (method, origin, clockwise/reverse), the radial amount, and the linear cut position
			// in the rect's own units.
			public RevealMethod RevealMethod;
			public int RevealHeader;
			public int RevealValue;
			public float RevealCut;

			/// <summary>Where the texture actually is, in the texture's own (rotated) frame around TextureCenter.</summary>
			public Vector2 TextureRegionMin;
			public Vector2 TextureRegionMax;
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			if (!TryResolveShapeBounds(out var center, out var halfSize, out var bulge)) {
				return;
			}

			// A zero reveal shows nothing at all.
			if (m_revealMethod != RevealMethod.None && m_revealAmount <= 0f) {
				return;
			}

			var shape = BuildShapeData(center, halfSize, bulge);

			foreach (var shadow in m_shadows) {
				if (shadow.Enabled && !shadow.Inset) {
					var spread = Mathf.Clamp(shadow.Spread, -MAX_DISTANCE, MAX_DISTANCE);

					var edges = shadow.Tuck ? -CalculateTuck(shadow.Offset, spread, shadow.Softness) : Vector4.zero;

					// Drop shadows are revealed in their own offset space, so they stay the shadow of the revealed shape.
					if (edges == Vector4.zero) {
						AddLayer(vh, FancyRectLayers.DropShadows, shape, LAYER_BAND, NO_INNER_EDGE_CODE, spread, shadow.Softness, shadow.Offset, shadow.Paint, shadow.UseTexture,
							true, Vector2.zero);
					} else {
						var shadowShape = WithEdgeOffsets(shape, edges, out var edgeShift);
						AddLayer(vh, FancyRectLayers.DropShadows, shadowShape, LAYER_BAND, NO_INNER_EDGE_CODE, spread, shadow.Softness, shadow.Offset + edgeShift, shadow.Paint, shadow.UseTexture,
							true, edgeShift);
					}
				}
			}

			// Layers under an opaque layer that shares their edge end a pixel early, so they can't show through
			// where both edges are anti-aliased.
			var edgeCovered = IsEdgeCoveredByOutline();

			if (m_backgroundEnabled) {
				var underFill = m_fillEnabled && IsOpaque(m_fill, true);
				AddLayer(vh, FancyRectLayers.Background, shape, LAYER_BAND, NO_INNER_EDGE_CODE, 0, 0, Vector2.zero, m_background, false,
					underlay: edgeCovered || underFill);
			}

			if (m_fillEnabled) {
				AddLayer(vh, FancyRectLayers.Fill, shape, LAYER_BAND, NO_INNER_EDGE_CODE, 0, 0, Vector2.zero, m_fill, true, underlay: edgeCovered);
			}

			foreach (var shadow in m_shadows) {
				if (shadow.Enabled && shadow.Inset) {
					var spread = Mathf.Clamp(shadow.Spread, 0, MAX_DISTANCE);
					AddLayer(vh, FancyRectLayers.InnerShadows, shape, LAYER_INNER_SHADOW, NO_INNER_EDGE_CODE, spread, shadow.Softness, shadow.Offset, shadow.Paint, shadow.UseTexture);
				}
			}

			if (m_bevel.Enabled && m_bevel.Width > 0f) {
				AddBevel(vh, shape);
			}

			if (m_gloss.Enabled && m_gloss.Color.a > 0f) {
				AddGloss(vh, shape);
			}

			foreach (var outline in m_outlines) {
				if (outline.Enabled && outline.Width > 0) {
					var band = outline.GetBand();
					var startCode = Signed12(Mathf.Max(band.x, -MAX_DISTANCE + 0.5f));
					var styled = outline.Style != OutlineStyle.Solid;

					AddLayer(vh, FancyRectLayers.Outlines, shape, styled ? LAYER_STYLED_BAND : LAYER_BAND, startCode, band.y, 0, Vector2.zero, outline.Paint,
						outline.UseTexture, styleCode: styled ? PackOutlineStyle(outline) : 0, extraReach: outline.GetStyleReach());
				}
			}
		}

		/// <summary>
		/// Whether an opaque, solid outline sits on the shape edge and reaches at least a unit inside it.
		/// </summary>
		private bool IsEdgeCoveredByOutline()
		{
			foreach (var outline in m_outlines) {
				if (!outline.Enabled || outline.Style != OutlineStyle.Solid || !IsOpaque(outline.Paint, outline.UseTexture)) {
					continue;
				}

				var band = outline.GetBand();
				if (band.x <= -1f && band.y >= 0f) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Whether a layer hides everything under it. A textured layer never counts, since the sprite can be transparent.
		/// </summary>
		private bool IsOpaque(in ShapePaint paint, bool textured)
		{
			if (textured && m_sprite != null) {
				return false;
			}

			if (color.a < 1f || paint.Color.a < 1f) {
				return false;
			}

			return paint.Gradient == GradientType.None || paint.Color2.a >= 1f;
		}

		/// <summary>
		/// Packs an outline's style into the 24 bits of uv2.z, low bits first. The shader decodes the same layouts.
		/// Dashes: style 2 | spacing 10 (0.5 unit steps) | length 6 | speed 5 (8 units/s steps, biased by 16) | round caps 1.
		/// Notches: style 2 | count 7 | width 6 | depth 4 | shape 2 | side 1 | jitter 2.
		/// Wobble: style 2 | wavelength 8 | thickness 5 (0.5 unit steps) | drift 5 (0.5 unit steps) | seed 4.
		/// </summary>
		private static int PackOutlineStyle(in ShapeOutline outline)
		{
			var code = (int)outline.Style;
			var shift = 2;

			void Put(int value, int bitCount)
			{
				code |= Mathf.Clamp(value, 0, (1 << bitCount) - 1) << shift;
				shift += bitCount;
			}

			switch (outline.Style) {
				case OutlineStyle.Dashes: {
					var dashes = outline.Dashes;
					Put(Mathf.Max(2, Mathf.RoundToInt(dashes.Spacing * 2f)), 10);
					Put(Mathf.RoundToInt(Mathf.Clamp01(dashes.Length) * 63f), 6);
					Put(Mathf.RoundToInt(dashes.Speed / 8f) + 16, 5);
					Put(dashes.RoundCaps ? 1 : 0, 1);
					break;
				}
				case OutlineStyle.Notches: {
					var notches = outline.Notches;
					Put(Mathf.Max(1, notches.Count), 7);
					Put(Mathf.Max(1, Mathf.RoundToInt(notches.Width)), 6);
					Put(Mathf.RoundToInt(Mathf.Clamp01(notches.Depth) * 15f), 4);
					Put((int)notches.Shape, 2);
					Put((int)notches.Side, 1);
					Put(Mathf.RoundToInt(Mathf.Clamp01(notches.Jitter) * 3f), 2);
					break;
				}
				case OutlineStyle.Wobble: {
					var wobble = outline.Wobble;
					Put(Mathf.Max(1, Mathf.RoundToInt(wobble.Wavelength)), 8);
					Put(Mathf.RoundToInt(wobble.Thickness * 2f), 5);
					Put(Mathf.RoundToInt(wobble.Drift * 2f), 5);
					Put(wobble.Seed, 4);
					break;
				}
			}

			return code;
		}

		private void AddBevel(VertexHelper vh, in ShapeData shape)
		{
			var width = Mathf.Clamp(m_bevel.Width, 0f, MAX_DISTANCE);

			// Highlight and shadow travel as the two gradient colors; the shader picks one from the edge's facing.
			var paint = new ShapePaint {
				Color = m_bevel.Highlight,
				Color2 = m_bevel.Shadow,
				Gradient = GradientType.Linear,
				Angle = m_bevel.LightAngle
			};

			AddLayer(vh, FancyRectLayers.Bevel, shape, LAYER_BEVEL, Signed12(-width), 0f, m_bevel.Softness, Vector2.zero, paint, false);
		}

		/// <summary>
		/// The gloss is a copy of the shape inset from the top and sides and cut off partway down, with concentric
		/// corners, drawn as a solid layer with a vertical gradient that fades toward its bottom.
		/// </summary>
		private void AddGloss(VertexHelper vh, in ShapeData shape)
		{
			var inset = Mathf.Max(0f, m_gloss.Inset);
			var size = shape.HalfSize * 2f;
			var glossHeight = Mathf.Max(0f, size.y - inset * 2f) * Mathf.Clamp01(m_gloss.Height);

			if (glossHeight < 0.5f || size.x - inset * 2f < 0.5f) {
				return;
			}

			// Top corners are concentric with the shape's (radius minus inset), which is what keeps the gloss inside
			// the fill. A corner needs a shape at least as tall as its radius, so a gloss shorter than that uses a
			// taller shape and only draws the part above its cut line.
			var topLeft = Mathf.Max(shape.Radii.x - inset, 0f);
			var topRight = Mathf.Max(shape.Radii.y - inset, 0f);
			var shapeHeight = Mathf.Max(glossHeight, Mathf.Max(topLeft, topRight));

			// The bottom corners sit mid-shape, so they mirror the top ones and give way when there isn't room for
			// both; that keeps the top concentric instead of letting the radius fitting shrink all four evenly.
			var bottomRight = Mathf.Clamp(shapeHeight - topRight, 0f, topRight);
			var bottomLeft = Mathf.Clamp(shapeHeight - topLeft, 0f, topLeft);
			var radii = new Vector4(topLeft, topRight, bottomRight, bottomLeft);

			var edges = new Vector4(-inset, -inset, -(size.y - inset - shapeHeight), -inset);
			var glossShape = WithEdgeOffsets(shape, edges, out var shift, radii);

			var faded = m_gloss.Color;
			faded.a *= 1f - Mathf.Clamp01(m_gloss.Fade);

			var paint = ShapePaint.TwoColor(faded, m_gloss.Color, GradientType.Linear, 0f);
			var clipBottom = float.NegativeInfinity;

			if (shapeHeight > glossHeight) {
				// The fade still ends at the cut line, and the quad stops there. That edge is the quad's own, so it
				// has no softness, which only shows when Fade leaves the bottom of the gloss visible.
				paint.RangeStart = 1f - glossHeight / shapeHeight;
				clipBottom = (shape.HalfSize.y * shape.InvScale.y - inset - glossHeight) / glossShape.InvScale.y;
			}

			AddLayer(vh, FancyRectLayers.Gloss, glossShape, LAYER_BAND, NO_INNER_EDGE_CODE, 0f, m_gloss.Softness, shift, paint, false,
				clipBottom: clipBottom);
		}

		/// <summary>
		/// Where the un-bulged shape sits. Normally that is the whole rect; with Contain Bulge each side is pulled
		/// in by its outward bulge, so the bulge's peak lands exactly on the rect edge.
		/// </summary>
		private bool TryResolveShapeBounds(out Vector2 center, out Vector2 halfSize, out Vector4 bulge)
		{
			var rect = GetPixelAdjustedRect();
			center = rect.center;
			halfSize = new Vector2(Mathf.Abs(rect.width), Mathf.Abs(rect.height)) * 0.5f;
			bulge = ClampBulge(m_bulge, halfSize);

			if (halfSize.x <= 0 || halfSize.y <= 0) {
				return false;
			}

			if (!m_containBulge) {
				return true;
			}

			// Outward bulge per side: top, right, bottom, left. Keep at least 10% of each axis for the shape itself.
			var outward = Vector4.Max(bulge, Vector4.zero);
			var scaleX = LimitScale(1f, halfSize.x * 2f * 0.9f, outward.y + outward.w);
			var scaleY = LimitScale(1f, halfSize.y * 2f * 0.9f, outward.x + outward.z);
			outward = new Vector4(outward.x * scaleY, outward.y * scaleX, outward.z * scaleY, outward.w * scaleX);

			center += new Vector2((outward.w - outward.y) * 0.5f, (outward.z - outward.x) * 0.5f);
			halfSize -= new Vector2((outward.y + outward.w) * 0.5f, (outward.x + outward.z) * 0.5f);

			// Scaled-down outward bulges replace the originals; inward ones are re-limited for the smaller shape.
			bulge = new Vector4(
				bulge.x > 0 ? outward.x : bulge.x,
				bulge.y > 0 ? outward.y : bulge.y,
				bulge.z > 0 ? outward.z : bulge.z,
				bulge.w > 0 ? outward.w : bulge.w);
			bulge = ClampBulge(bulge, halfSize);

			return true;
		}

		/// <summary>
		/// How far to pull in each edge (top, right, bottom, left) of a drop shadow so nothing shows past the shape
		/// on the sides facing away from its offset. A side is only tucked when its softness would actually leak;
		/// sides along the offset direction (and every side of a zero-offset glow) are left alone.
		/// </summary>
		private static Vector4 CalculateTuck(Vector2 offset, float spread, float softness)
		{
			// How far the soft edge reaches past the shape on a side, when the shadow sits `shift` toward it.
			float Leak(float shift) => Mathf.Max(0f, shift + spread + softness * SOFTNESS_REACH + TUCK_MARGIN);

			return new Vector4(
				offset.y < 0f ? Leak(offset.y) : 0f,
				offset.x < 0f ? Leak(offset.x) : 0f,
				offset.y > 0f ? Leak(-offset.y) : 0f,
				offset.x > 0f ? Leak(-offset.x) : 0f);
		}

		private static Vector4 PackShapeUv1(Vector4 radii, Vector4 bulge)
		{
			return new Vector4(
				Pack(Unsigned12(radii.x), Unsigned12(radii.y)),
				Pack(Unsigned12(radii.z), Unsigned12(radii.w)),
				Pack(Signed12(bulge.x), Signed12(bulge.z)),
				Pack(Signed12(bulge.y), Signed12(bulge.w)));
		}

		/// <summary>
		/// A copy of the shape with each edge (top, right, bottom, left) moved outward by the given amount, used to
		/// tuck a drop shadow. Returned as a resized shape plus a shift of its center, which the caller adds to the offset.
		/// Corner radii stay the same (refitted if the shadow got smaller); the texture mapping is unchanged.
		/// </summary>
		private static ShapeData WithEdgeOffsets(in ShapeData shape, Vector4 edges, out Vector2 shift, Vector4? radii = null)
		{
			var size = Vector2.Scale(shape.HalfSize, shape.InvScale) + new Vector2((edges.y + edges.w) * 0.5f, (edges.x + edges.z) * 0.5f);

			var result = shape;
			SetHalfSize(ref result, size);

			// The shift is used as a layer offset, which is in the new shape's own space.
			shift = new Vector2((edges.y - edges.w) * 0.5f / result.InvScale.x, (edges.x - edges.z) * 0.5f / result.InvScale.y);

			result.Radii = FitRadii(radii ?? shape.Radii, result.HalfSize * 2f);
			result.Bulge = ClampBulge(shape.Bulge, result.HalfSize);
			result.Uv1 = PackShapeUv1(result.Radii, result.Bulge);

			return result;
		}

		/// <summary>
		/// Sets a shape's half size from its real size in the rect: rounds it to what the vertex data can encode and
		/// records the scale that maps the rounded shape back onto the real one.
		/// </summary>
		private static void SetHalfSize(ref ShapeData shape, Vector2 halfSize)
		{
			halfSize = Vector2.Max(halfSize, new Vector2(0.5f, 0.5f));

			var codeX = HalfSizeCode(halfSize.x);
			var codeY = HalfSizeCode(halfSize.y);

			shape.HalfSize = new Vector2(DecodeHalfSize(codeX), DecodeHalfSize(codeY));
			shape.InvScale = new Vector2(halfSize.x / shape.HalfSize.x, halfSize.y / shape.HalfSize.y);
			shape.PackedHalfSize = Pack(codeX, codeY);
		}

		/// <param name="forMesh">False skips the texture mapping, which only the mesh needs.</param>
		private ShapeData BuildShapeData(Vector2 center, Vector2 halfSize, Vector4 bulge, bool forMesh = true)
		{
			var shape = new ShapeData {
				Center = center
			};

			SetHalfSize(ref shape, halfSize);

			var cornerTypes = GetCornerTypes();

			shape.Radii = Vector4.Min(CalculateRadii(shape.HalfSize), Vector4.one * (MAX_DISTANCE * 2f));
			shape.Bulge = ClampBulge(bulge, shape.HalfSize);
			shape.Uv1 = PackShapeUv1(shape.Radii, shape.Bulge);
			shape.CornerBits = (int)cornerTypes.TopLeft
			                   | ((int)cornerTypes.TopRight << 2)
			                   | ((int)cornerTypes.BottomRight << 4)
			                   | ((int)cornerTypes.BottomLeft << 6);

			if (forMesh) {
				SetupTextureMapping(ref shape);
			}

			SetupReveal(ref shape);
			return shape;
		}

		/// <summary>
		/// Precomputes how local positions map to texture UVs. Texture UVs are written per vertex, which is exact
		/// because the mapping is linear across each layer quad, and lets atlased sprites work.
		/// </summary>
		private void SetupTextureMapping(ref ShapeData shape)
		{
			var radians = m_textureRotation * Mathf.Deg2Rad;
			shape.TextureCos = Mathf.Cos(radians);
			shape.TextureSin = Mathf.Sin(radians);
			shape.TextureMode = m_textureMode;
			shape.TextureOffset = m_textureOffset;

			// Scrolling wraps by a whole texture, which only looks seamless on a repeating tile.
			var scroll = m_textureMode == ShapeTextureMode.Tile ? m_textureScroll : Vector2.zero;
			shape.PackedScroll = Pack(Scroll12(scroll.x), Scroll12(scroll.y));
			shape.OuterUv = m_sprite != null ? UnityEngine.Sprites.DataUtility.GetOuterUV(m_sprite) : new Vector4(0, 0, 1, 1);

			var scale = new Vector2(SafeScale(m_textureScale.x), SafeScale(m_textureScale.y));

			if (m_textureMode == ShapeTextureMode.Tile) {
				var tileSize = shape.HalfSize * 2f;

				if (m_sprite != null) {
					var referencePixelsPerUnit = canvas != null ? canvas.referencePixelsPerUnit : 100f;
					tileSize = m_sprite.rect.size * (referencePixelsPerUnit / Mathf.Max(0.0001f, m_sprite.pixelsPerUnit));
				}

				shape.TextureCenter = Vector2.zero;
				shape.TextureDivisor = Vector2.Scale(tileSize, scale);
			} else {
				// Fit the texture to the visible shape, including any outward bulge.
				var bulgeOut = Vector4.Max(shape.Bulge, Vector4.zero);
				var boundsMin = new Vector2(-shape.HalfSize.x - bulgeOut.w, -shape.HalfSize.y - bulgeOut.z);
				var boundsMax = new Vector2(shape.HalfSize.x + bulgeOut.y, shape.HalfSize.y + bulgeOut.x);

				var aspect = 1f;
				if (m_sprite != null && m_sprite.rect.height > 0) {
					aspect = m_sprite.rect.width / m_sprite.rect.height;
				}

				shape.TextureCenter = (boundsMin + boundsMax) * 0.5f;
				shape.TextureDivisor = Vector2.Scale(FitTextureSize(boundsMax - boundsMin, aspect, m_textureMode), scale);
			}

			shape.TextureDivisor = new Vector2(NonZero(shape.TextureDivisor.x), NonZero(shape.TextureDivisor.y));

			// normalized = frame / divisor + 0.5 + offset is inside 0-1 where the texture is.
			var regionA = Vector2.Scale(new Vector2(-0.5f, -0.5f) - shape.TextureOffset, shape.TextureDivisor);
			var regionB = Vector2.Scale(new Vector2(0.5f, 0.5f) - shape.TextureOffset, shape.TextureDivisor);
			shape.TextureRegionMin = Vector2.Min(regionA, regionB) - Vector2.one * TEXTURE_EDGE_MARGIN;
			shape.TextureRegionMax = Vector2.Max(regionA, regionB) + Vector2.one * TEXTURE_EDGE_MARGIN;
		}

		/// <summary>
		/// Precomputes the reveal. Linear reveals are baked into a cut position in the rect's own units, so layers
		/// drawn with a resized copy of the shape (gloss, tucked shadows) still cut in the same place.
		/// </summary>
		private void SetupReveal(ref ShapeData shape)
		{
			shape.RevealMethod = m_revealAmount >= 1f ? RevealMethod.None : m_revealMethod;

			if (shape.RevealMethod == RevealMethod.None) {
				return;
			}

			var amount = Mathf.Clamp01(m_revealAmount);
			var outward = Vector4.Max(shape.Bulge, Vector4.zero); // top, right, bottom, left
			var origin = m_revealOrigin;

			switch (shape.RevealMethod) {
				case RevealMethod.Horizontal: {
					var min = -shape.HalfSize.x - outward.w;
					var max = shape.HalfSize.x + outward.y;
					var fromRight = origin == RevealOrigin.Right;
					shape.RevealCut = (fromRight ? Mathf.Lerp(max, min, amount) : Mathf.Lerp(min, max, amount)) * shape.InvScale.x;
					shape.RevealHeader = (int)RevealMethod.Horizontal | (fromRight ? REVEAL_CLOCKWISE_BIT : 0);
					break;
				}
				case RevealMethod.Vertical: {
					var min = -shape.HalfSize.y - outward.z;
					var max = shape.HalfSize.y + outward.x;
					var fromTop = origin == RevealOrigin.Top;
					shape.RevealCut = (fromTop ? Mathf.Lerp(max, min, amount) : Mathf.Lerp(min, max, amount)) * shape.InvScale.y;
					shape.RevealHeader = (int)RevealMethod.Vertical | (fromTop ? REVEAL_CLOCKWISE_BIT : 0);
					break;
				}
				default:
					shape.RevealHeader = (int)RevealMethod.Radial | ((int)origin << 2) | (m_revealClockwise ? REVEAL_CLOCKWISE_BIT : 0);
					shape.RevealValue = Mathf.Clamp(Mathf.RoundToInt(amount * 65535f), 0, 65535);
					break;
			}
		}

		/// <summary>
		/// A cut position as a sign bit and 15 bits: 1/16 unit steps up to 1024 units from the center, then 0.5 unit
		/// steps, which reaches past the largest shape.
		/// </summary>
		private static int CutCode(float cut)
		{
			var distance = Mathf.Abs(cut);
			var code = distance <= 1024f
				? Mathf.RoundToInt(distance * 16f)
				: 16384 + Mathf.RoundToInt((distance - 1024f) * 2f);

			return Mathf.Min(code, 32767) | (cut < 0f ? 32768 : 0);
		}

		/// <param name="offsetSpace">Reveal in the layer's offset space (drop shadows).</param>
		/// <param name="shapeShift">How far this layer's shape was moved on top of its offset (tuck); undone for linear cuts.</param>
		private static float PackReveal(in ShapeData shape, bool offsetSpace, Vector2 shapeShift)
		{
			if (shape.RevealMethod == RevealMethod.None) {
				return 0f;
			}

			var value = shape.RevealValue;

			// The cut is kept in the rect's units; each layer needs it in its own shape's space.
			if (shape.RevealMethod == RevealMethod.Horizontal) {
				value = CutCode(shape.RevealCut / shape.InvScale.x - (offsetSpace ? shapeShift.x : 0f));
			} else if (shape.RevealMethod == RevealMethod.Vertical) {
				value = CutCode(shape.RevealCut / shape.InvScale.y - (offsetSpace ? shapeShift.y : 0f));
			}

			return shape.RevealHeader | (value << REVEAL_VALUE_SHIFT) | (offsetSpace ? REVEAL_OFFSET_SPACE_BIT : 0);
		}

		/// <summary>Whether a point relative to the shape center, in the rect's units, is inside the revealed part. Matches the shader.</summary>
		private bool IsRevealed(Vector2 p, in ShapeData shape)
		{
			switch (shape.RevealMethod) {
				case RevealMethod.None:
					return true;
				case RevealMethod.Horizontal:
					return m_revealOrigin == RevealOrigin.Right ? p.x >= shape.RevealCut : p.x <= shape.RevealCut;
				case RevealMethod.Vertical:
					return m_revealOrigin == RevealOrigin.Top ? p.y >= shape.RevealCut : p.y <= shape.RevealCut;
			}

			var startAngle = m_revealOrigin switch {
				RevealOrigin.Top => 90f,
				RevealOrigin.Right => 0f,
				RevealOrigin.Bottom => 270f,
				_ => 180f
			};

			var angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
			var swept = Mathf.Repeat(m_revealClockwise ? startAngle - angle : angle - startAngle, 360f);
			return swept <= Mathf.Clamp01(m_revealAmount) * 360f;
		}

		/// <summary>
		/// Size of the texture, in local units, for Stretch and Cover.
		/// </summary>
		private static Vector2 FitTextureSize(Vector2 bounds, float aspect, ShapeTextureMode mode)
		{
			if (mode != ShapeTextureMode.Cover) {
				return bounds;
			}

			// Match whichever side leaves the texture covering the other one too.
			var boundsAspect = bounds.x / Mathf.Max(bounds.y, 0.0001f);
			return boundsAspect > aspect
				? new Vector2(bounds.x, bounds.x / aspect)
				: new Vector2(bounds.y * aspect, bounds.y);
		}

		private static float SafeScale(float value)
		{
			return Mathf.Abs(value) < 0.0001f ? 0.0001f : value;
		}

		private static float NonZero(float value)
		{
			return Mathf.Abs(value) < 0.0001f ? (value < 0 ? -0.0001f : 0.0001f) : value;
		}

		/// <summary>Local position (relative to the texture origin) into the texture's rotated frame.</summary>
		private static Vector2 ToTextureFrame(in ShapeData shape, Vector2 local)
		{
			var p = local - shape.TextureCenter;

			// Rotate the opposite way so the texture appears rotated by +angle.
			return new Vector2(
				p.x * shape.TextureCos + p.y * shape.TextureSin,
				-p.x * shape.TextureSin + p.y * shape.TextureCos);
		}

		private static Vector2 FromTextureFrame(in ShapeData shape, Vector2 frame)
		{
			return shape.TextureCenter + new Vector2(
				frame.x * shape.TextureCos - frame.y * shape.TextureSin,
				frame.x * shape.TextureSin + frame.y * shape.TextureCos);
		}

		private static Vector2 CalculateTextureUv(in ShapeData shape, Vector2 local)
		{
			var frame = ToTextureFrame(shape, local);

			var normalized = new Vector2(frame.x / shape.TextureDivisor.x, frame.y / shape.TextureDivisor.y)
			                 + new Vector2(0.5f, 0.5f) + shape.TextureOffset;

			if (shape.TextureMode == ShapeTextureMode.Tile) {
				return normalized;
			}

			var uv = shape.OuterUv;
			return new Vector2(Mathf.LerpUnclamped(uv.x, uv.z, normalized.x), Mathf.LerpUnclamped(uv.y, uv.w, normalized.y));
		}

		/// <param name="startCode">Encoded inner edge of the band; NO_INNER_EDGE_CODE for a solid layer.</param>
		/// <param name="end">Outer edge of the band (distance from the shape edge), or the spread for inner shadows.</param>
		/// <param name="styleCode">Packed outline style, sent in place of the offset by LAYER_STYLED_BAND layers.</param>
		/// <param name="extraReach">How far past the band end the layer can draw (wobble).</param>
		/// <param name="underlay">An opaque layer above shares this layer's outer edge, so it ends a pixel early.</param>
		/// <param name="clipBottom">Nothing is drawn below this height, relative to the shape center (a short gloss).</param>
		private void AddLayer(VertexHelper vh, FancyRectLayers layerKind, in ShapeData shape, int layerType, int startCode, float end, float softness,
			Vector2 offset, ShapePaint paint, bool textured, bool revealInOffsetSpace = false, Vector2 revealShapeShift = default,
			int styleCode = 0, float extraReach = 0f, bool underlay = false, float clipBottom = float.NegativeInfinity)
		{
			softness = Mathf.Clamp(softness, 0, MAX_DISTANCE * 2f);
			offset = new Vector2(Mathf.Clamp(offset.x, -MAX_DISTANCE, MAX_DISTANCE), Mathf.Clamp(offset.y, -MAX_DISTANCE, MAX_DISTANCE));

			// How far past the shape edge this layer can draw. Inner shadows and bevels never leave the shape.
			var insideOnly = layerType == LAYER_INNER_SHADOW || layerType == LAYER_BEVEL;
			var reach = insideOnly ? EDGE_PADDING : (Mathf.Max(end + extraReach, 0) + softness * SOFTNESS_REACH) * BulgeStretch(shape) + EDGE_PADDING;

			// The quad moves with the offset, so it only covers where the offset shape is.
			var quadOffset = layerType == LAYER_INNER_SHADOW ? Vector2.zero : offset;

			var h = shape.HalfSize;
			var b = shape.Bulge;
			var left = h.x + Mathf.Max(b.w, 0) + reach - quadOffset.x;
			var right = h.x + Mathf.Max(b.y, 0) + reach + quadOffset.x;
			var top = h.y + Mathf.Max(b.x, 0) + reach + quadOffset.y;
			var bottom = h.y + Mathf.Max(b.z, 0) + reach - quadOffset.y;

			var tint = color;
			var color1 = (Color32)(paint.Color * tint);
			var color2 = paint.Gradient == GradientType.None ? color1 : (Color32)(paint.Color2 * tint);

			// The vertex color is white with the anchor's alpha, so the shader receives the CanvasRenderer color
			// directly in rgb. The anchor (the color with more alpha) travels in tangent.w and the other color in uv3,
			// its alpha stored relative to the anchor's so the CanvasRenderer alpha scales both.
			var anchorIsColor2 = color2.a > color1.a;
			var anchor = anchorIsColor2 ? color2 : color1;
			var other = anchorIsColor2 ? color1 : color2;
			var otherAlphaCode = anchor.a == 0 ? 0 : Mathf.Min(255, Mathf.RoundToInt(other.a * 255f / anchor.a));
			var tintBits = ((m_rendererTintLayers & layerKind) == 0 ? IGNORE_RENDERER_TINT_BIT : 0)
			               | (anchorIsColor2 ? ANCHOR_IS_COLOR2_BIT : 0);

			// Gradient shaping, 8 bits each: start and end in uv3.y above the alpha, bias in the spare flag bits.
			var rangeStart = Mathf.Clamp01(paint.RangeStart);
			var rangeEnd = Mathf.Clamp01(paint.RangeEnd);
			var gradientStartCode = Mathf.RoundToInt(Mathf.Min(rangeStart, rangeEnd) * 255f);
			var gradientEndCode = Mathf.RoundToInt(Mathf.Max(rangeStart, rangeEnd) * 255f);
			var gradientBiasCode = Mathf.RoundToInt((Mathf.Clamp(paint.Bias, -1f, 1f) * 0.5f + 0.5f) * 255f);

			var solid = layerType == LAYER_BAND && startCode == NO_INNER_EDGE_CODE && Signed12(end) == 2048;

			var flags = layerType | ((int)paint.Gradient << 2) | (shape.CornerBits << 5) | (underlay ? FLAG_UNDERLAY : 0) | (gradientBiasCode << 15)
			            | (solid ? FLAG_SOLID : 0);
			var angleCode = Mathf.Clamp(Mathf.RoundToInt(Mathf.Repeat(paint.Angle, 360f) / 360f * 4095f), 0, 4095);

			var plainUv2 = new Vector4(
				solid ? shape.PackedScroll : Pack(startCode, Signed12(end)),
				Pack(Unsigned12(softness), angleCode),
				layerType == LAYER_STYLED_BAND ? styleCode : Pack(Signed12(offset.x), Signed12(offset.y)),
				flags);

			var texturedUv2 = new Vector4(plainUv2.x, plainUv2.y, plainUv2.z, flags | FLAG_TEXTURED);
			var uv3 = new Vector4(
				(other.r << 16) | (other.g << 8) | other.b,
				otherAlphaCode | (gradientStartCode << 8) | (gradientEndCode << 16),
				shape.PackedHalfSize,
				(int)PackReveal(shape, revealInOffsetSpace, revealShapeShift) | tintBits);

			var layer = new LayerVertexData {
				// A textured drop shadow carries its texture along with its offset; inner shadows stay with the fill.
				TextureOrigin = layerType == LAYER_INNER_SHADOW ? Vector2.zero : offset,
				Color = new Color32(255, 255, 255, anchor.a),
				AnchorRgb = (anchor.r << 16) | (anchor.g << 8) | anchor.b,
				Uv3 = uv3
			};

			var quadMin = new Vector2(-left, Mathf.Max(-bottom, clipBottom));
			var quadMax = new Vector2(right, top);

			if (TryGetLayerHole(shape, layerType, startCode, end, softness, extraReach, offset, quadMin, quadMax, out var holeMin, out var holeMax)) {
				EmitRing(vh, shape, layer, quadMin, quadMax, holeMin, holeMax, plainUv2, texturedUv2, textured);
			} else {
				EmitLayerRect(vh, shape, layer, quadMin, quadMax, plainUv2, texturedUv2, textured);
			}
		}

		/// <summary>
		/// Layers that only draw near the edge (outlines, bevels, inner shadows) leave the middle of the shape empty.
		/// Finds that empty middle as a rect, so the layer can be drawn as a ring around it and the GPU never shades
		/// those pixels. Returns false for layers that fill the shape, or when the hole is too small to be worth
		/// the extra quads.
		/// </summary>
		private static bool TryGetLayerHole(in ShapeData shape, int layerType, int startCode, float end, float softness, float extraReach,
			Vector2 offset, Vector2 quadMin, Vector2 quadMax, out Vector2 holeMin, out Vector2 holeMax)
		{
			holeMin = holeMax = Vector2.zero;

			float innerEdge;
			if (layerType == LAYER_INNER_SHADOW) {
				// Shadowed where the offset shape, shrunk by the spread, is not.
				innerEdge = -end;
			} else if (startCode != NO_INNER_EDGE_CODE) {
				// Decoded the way the shader decodes it, so the hole matches what is drawn.
				innerEdge = (startCode - 2048) * 0.25f;
			} else {
				return false;
			}

			// The anti-aliased edge and any wobble can reach past the inner edge, so stay clear of both.
			var depth = Mathf.Max(0f, (-innerEdge + extraReach + softness * SOFTNESS_REACH) * BulgeStretch(shape) + EDGE_PADDING);

			if (!TryGetInnerRect(shape, depth, out holeMin, out holeMax)) {
				return false;
			}

			// Band layers are drawn against the shape moved by the offset (only inner shadows have one).
			holeMin = Vector2.Max(holeMin + offset, quadMin);
			holeMax = Vector2.Min(holeMax + offset, quadMax);

			var holeSize = holeMax - holeMin;
			var quadSize = quadMax - quadMin;

			return holeSize.x >= MIN_RING_HOLE_SIZE && holeSize.y >= MIN_RING_HOLE_SIZE
			       && holeSize.x * holeSize.y >= quadSize.x * quadSize.y * MIN_RING_HOLE_FRACTION;
		}

		/// <summary>
		/// The shader measures distances on a bulged side to first order, so far from the edge a layer can reach a
		/// little further than its width. This is the factor that keeps quads and ring holes clear of that: 1 with
		/// no bulge, growing with how steep the steepest bulge is.
		/// </summary>
		private static float BulgeStretch(in ShapeData shape)
		{
			var b = shape.Bulge;
			var h = shape.HalfSize;

			var topBottom = Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.z)) / Mathf.Max(h.x, 0.0001f);
			var leftRight = Mathf.Max(Mathf.Abs(b.y), Mathf.Abs(b.w)) / Mathf.Max(h.y, 0.0001f);

			return 1f + Mathf.Max(topBottom, leftRight) * Mathf.PI * 0.5f;
		}

		/// <summary>
		/// The rect (relative to the shape center) whose every point is at least `depth` inside the shape.
		/// Accounts for each corner's radius and type, and for sides bulging inward.
		/// </summary>
		private static bool TryGetInnerRect(in ShapeData shape, float depth, out Vector2 min, out Vector2 max)
		{
			var radii = shape.Radii;
			var bits = shape.CornerBits;

			var topLeft = CornerInset(radii.x, (CornerType)(bits & 3), depth);
			var topRight = CornerInset(radii.y, (CornerType)((bits >> 2) & 3), depth);
			var bottomRight = CornerInset(radii.z, (CornerType)((bits >> 4) & 3), depth);
			var bottomLeft = CornerInset(radii.w, (CornerType)((bits >> 6) & 3), depth);

			// An inward bulge moves the side in by at most its amount: top, right, bottom, left.
			var inward = Vector4.Max(-shape.Bulge, Vector4.zero);

			var h = shape.HalfSize;
			min = new Vector2(-h.x + Mathf.Max(bottomLeft, topLeft) + inward.w, -h.y + Mathf.Max(bottomRight, bottomLeft) + inward.z);
			max = new Vector2(h.x - Mathf.Max(topRight, bottomRight) - inward.y, h.y - Mathf.Max(topLeft, topRight) - inward.x);

			return max.x > min.x && max.y > min.y;
		}

		/// <summary>
		/// How far in from both of a corner's sides a point must be to sit `depth` inside the shape there.
		/// Solved from each corner type's distance function at the point (-k, -k) from the corner.
		/// </summary>
		private static float CornerInset(float radius, CornerType cornerType, float depth)
		{
			const float invSqrt2 = 0.70710678f;

			return cornerType switch {
				CornerType.Chamfer => Mathf.Max(depth, 0.5f * radius + invSqrt2 * depth),
				CornerType.Inverted => Mathf.Max(depth, invSqrt2 * (radius + depth)),
				// Round. Squircles are fuller than circles, so the round bound is safe for them too.
				_ => Mathf.Max(depth, (1f - invSqrt2) * radius + invSqrt2 * depth)
			};
		}

		/// <summary>Draws a layer as four quads around a hole: full-width bottom and top, then left and right between them.</summary>
		private static void EmitRing(VertexHelper vh, in ShapeData shape, in LayerVertexData layer, Vector2 quadMin, Vector2 quadMax,
			Vector2 holeMin, Vector2 holeMax, Vector4 plainUv2, Vector4 texturedUv2, bool textured)
		{
			EmitLayerRectIfNotEmpty(vh, shape, layer, quadMin, new Vector2(quadMax.x, holeMin.y), plainUv2, texturedUv2, textured);
			EmitLayerRectIfNotEmpty(vh, shape, layer, new Vector2(quadMin.x, holeMax.y), quadMax, plainUv2, texturedUv2, textured);
			EmitLayerRectIfNotEmpty(vh, shape, layer, new Vector2(quadMin.x, holeMin.y), new Vector2(holeMin.x, holeMax.y), plainUv2, texturedUv2, textured);
			EmitLayerRectIfNotEmpty(vh, shape, layer, new Vector2(holeMax.x, holeMin.y), new Vector2(quadMax.x, holeMax.y), plainUv2, texturedUv2, textured);
		}

		private static void EmitLayerRectIfNotEmpty(VertexHelper vh, in ShapeData shape, in LayerVertexData layer, Vector2 min, Vector2 max,
			Vector4 plainUv2, Vector4 texturedUv2, bool textured)
		{
			if (max.x - min.x > 0.0001f && max.y - min.y > 0.0001f) {
				EmitLayerRect(vh, shape, layer, min, max, plainUv2, texturedUv2, textured);
			}
		}

		private static void EmitLayerRect(VertexHelper vh, in ShapeData shape, in LayerVertexData layer, Vector2 min, Vector2 max,
			Vector4 plainUv2, Vector4 texturedUv2, bool textured)
		{
			if (!textured) {
				EmitRect(vh, shape, layer, min, max, plainUv2);
			} else if (shape.TextureMode == ShapeTextureMode.Tile) {
				EmitRect(vh, shape, layer, min, max, texturedUv2);
			} else {
				EmitSplitByTexture(vh, shape, layer, min, max, plainUv2, texturedUv2);
			}
		}

		private struct LayerVertexData
		{
			public Vector2 TextureOrigin;
			public Color32 Color;
			public int AnchorRgb;
			public Vector4 Uv3;
		}

		/// <summary>
		/// Splits a textured layer so it only samples the texture where the texture is. Any part of an outline or
		/// shadow beyond the texture (or of the fill, when Scale or Offset leave a gap) uses the layer's plain paint
		/// instead of stretched edge pixels or neighbouring atlas sprites.
		/// The split is done in the texture's rotated frame: one textured rect plus up to four plain bars.
		/// </summary>
		private static void EmitSplitByTexture(VertexHelper vh, in ShapeData shape, in LayerVertexData layer, Vector2 quadMin, Vector2 quadMax,
			Vector4 plainUv2, Vector4 texturedUv2)
		{
			var boundsMin = new Vector2(float.MaxValue, float.MaxValue);
			var boundsMax = new Vector2(float.MinValue, float.MinValue);

			IncludeInFrameBounds(shape, layer, quadMin, ref boundsMin, ref boundsMax);
			IncludeInFrameBounds(shape, layer, new Vector2(quadMin.x, quadMax.y), ref boundsMin, ref boundsMax);
			IncludeInFrameBounds(shape, layer, quadMax, ref boundsMin, ref boundsMax);
			IncludeInFrameBounds(shape, layer, new Vector2(quadMax.x, quadMin.y), ref boundsMin, ref boundsMax);

			var innerMin = Vector2.Max(boundsMin, shape.TextureRegionMin);
			var innerMax = Vector2.Min(boundsMax, shape.TextureRegionMax);

			if (innerMin.x >= innerMax.x || innerMin.y >= innerMax.y) {
				EmitFrameRect(vh, shape, layer, boundsMin, boundsMax, plainUv2);
				return;
			}

			EmitFrameRect(vh, shape, layer, innerMin, innerMax, texturedUv2);

			// Left and right bars span the full height; bottom and top bars fill the gaps between them.
			EmitFrameRect(vh, shape, layer, boundsMin, new Vector2(innerMin.x, boundsMax.y), plainUv2);
			EmitFrameRect(vh, shape, layer, new Vector2(innerMax.x, boundsMin.y), boundsMax, plainUv2);
			EmitFrameRect(vh, shape, layer, new Vector2(innerMin.x, boundsMin.y), new Vector2(innerMax.x, innerMin.y), plainUv2);
			EmitFrameRect(vh, shape, layer, new Vector2(innerMin.x, innerMax.y), new Vector2(innerMax.x, boundsMax.y), plainUv2);
		}

		private static void IncludeInFrameBounds(in ShapeData shape, in LayerVertexData layer, Vector2 corner, ref Vector2 boundsMin, ref Vector2 boundsMax)
		{
			var frame = ToTextureFrame(shape, corner - layer.TextureOrigin);
			boundsMin = Vector2.Min(boundsMin, frame);
			boundsMax = Vector2.Max(boundsMax, frame);
		}

		/// <summary>Emits a rect given in the texture frame (skipped when empty).</summary>
		private static void EmitFrameRect(VertexHelper vh, in ShapeData shape, in LayerVertexData layer, Vector2 min, Vector2 max, Vector4 uv2)
		{
			if (max.x - min.x <= 0.0001f || max.y - min.y <= 0.0001f) {
				return;
			}

			var origin = layer.TextureOrigin;
			EmitQuad(vh, shape, layer, uv2,
				FromTextureFrame(shape, min) + origin,
				FromTextureFrame(shape, new Vector2(min.x, max.y)) + origin,
				FromTextureFrame(shape, max) + origin,
				FromTextureFrame(shape, new Vector2(max.x, min.y)) + origin);
		}

		private static void EmitRect(VertexHelper vh, in ShapeData shape, in LayerVertexData layer, Vector2 min, Vector2 max, Vector4 uv2)
		{
			EmitQuad(vh, shape, layer, uv2, min, new Vector2(min.x, max.y), max, new Vector2(max.x, min.y));
		}

		private static void EmitQuad(VertexHelper vh, in ShapeData shape, in LayerVertexData layer, Vector4 uv2,
			Vector2 a, Vector2 b, Vector2 c, Vector2 d)
		{
			var startIndex = vh.currentVertCount;

			AddVertex(vh, shape, layer, a, uv2);
			AddVertex(vh, shape, layer, b, uv2);
			AddVertex(vh, shape, layer, c, uv2);
			AddVertex(vh, shape, layer, d, uv2);

			vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
			vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		private static void AddVertex(VertexHelper vh, in ShapeData shape, in LayerVertexData layer, Vector2 local, Vector4 uv2)
		{
			var textureUv = CalculateTextureUv(shape, local - layer.TextureOrigin);

			var vertex = UIVertex.simpleVert;
			vertex.position = shape.Center + Vector2.Scale(local, shape.InvScale);
			vertex.color = layer.Color;
			vertex.uv0 = new Vector4(local.x, local.y, textureUv.x, textureUv.y);
			vertex.uv1 = shape.Uv1;
			vertex.uv2 = uv2;
			vertex.uv3 = layer.Uv3;
			// Only w carries data: the canvas may rotate xyz with the element, but leaves w alone.
			vertex.tangent = new Vector4(1f, 0f, 0f, layer.AnchorRgb);
			vh.AddVert(vertex);
		}

		private Vector4 CalculateRadii(Vector2 halfSize)
		{
			var radii = m_radiusMode switch {
				RadiusMode.Pill => Vector4.one * Mathf.Min(halfSize.x, halfSize.y),
				RadiusMode.PerCorner => m_cornerRadii,
				_ => Vector4.one * m_radius
			};

			radii = MaskRadii(radii, m_roundedCorners);

			return FitRadii(radii, halfSize * 2f);
		}

		/// <summary>
		/// Zeroes the radii (TL, TR, BR, BL) of corners that aren't rounded.
		/// </summary>
		private static Vector4 MaskRadii(Vector4 radii, RectCorners corners)
		{
			if (corners == RectCorners.All) {
				return radii;
			}

			if ((corners & RectCorners.TopLeft) == 0) {
				radii.x = 0;
			}

			if ((corners & RectCorners.TopRight) == 0) {
				radii.y = 0;
			}

			if ((corners & RectCorners.BottomRight) == 0) {
				radii.z = 0;
			}

			if ((corners & RectCorners.BottomLeft) == 0) {
				radii.w = 0;
			}

			return radii;
		}

		private CornerTypes GetCornerTypes()
		{
			return m_radiusMode == RadiusMode.PerCorner ? m_cornerTypes : new CornerTypes(m_cornerType);
		}

		/// <summary>
		/// Scales radii (TL, TR, BR, BL) down so that adjacent corners never overlap.
		/// </summary>
		private static Vector4 FitRadii(Vector4 radii, Vector2 size)
		{
			radii = Vector4.Max(radii, Vector4.zero);

			var scale = 1f;
			scale = LimitScale(scale, size.x, radii.x + radii.y);
			scale = LimitScale(scale, size.x, radii.z + radii.w);
			scale = LimitScale(scale, size.y, radii.x + radii.w);
			scale = LimitScale(scale, size.y, radii.y + radii.z);

			return radii * scale;
		}

		private static float LimitScale(float scale, float side, float radiusSum)
		{
			return radiusSum > side ? Mathf.Min(scale, side / radiusSum) : scale;
		}

		/// <summary>
		/// Keeps concave bulges from pushing an edge past the center, and everything inside the encodable range.
		/// </summary>
		private static Vector4 ClampBulge(Vector4 bulge, Vector2 halfSize)
		{
			var maxInX = halfSize.x * 0.9f;
			var maxInY = halfSize.y * 0.9f;

			return new Vector4(
				Mathf.Clamp(bulge.x, -maxInY, MAX_DISTANCE),
				Mathf.Clamp(bulge.y, -maxInX, MAX_DISTANCE),
				Mathf.Clamp(bulge.z, -maxInY, MAX_DISTANCE),
				Mathf.Clamp(bulge.w, -maxInX, MAX_DISTANCE));
		}

		private static float Pack(int high, int low)
		{
			return high * 4096 + low;
		}

		private static int Unsigned12(float value)
		{
			return Mathf.Clamp(Mathf.RoundToInt(value * 4f), 0, 4095);
		}

		private static int Scroll12(float value)
		{
			return Mathf.Clamp(Mathf.RoundToInt(value * 256f) + 2048, 0, 4095);
		}

		/// <summary>
		/// A half size in 12 bits: a 3-bit exponent above a 9-bit mantissa, in 1/8 unit steps doubled per exponent.
		/// That keeps the rounding under 0.2% at any size, which InvScale then hides. Never zero.
		/// </summary>
		private static int HalfSizeCode(float value)
		{
			value = Mathf.Clamp(value, 0.125f, MAX_HALF_SIZE);

			var exponent = 0;
			var step = 0.125f;

			while (exponent < 7 && value > 511f * step) {
				exponent++;
				step *= 2f;
			}

			return (exponent << 9) | Mathf.Clamp(Mathf.RoundToInt(value / step), 1, 511);
		}

		private static float DecodeHalfSize(int code)
		{
			return (code & 511) * 0.125f * (1 << (code >> 9));
		}

		private static int Signed12(float value)
		{
			return Mathf.Clamp(Mathf.RoundToInt(value * 4f) + 2048, 0, 4095);
		}

		public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
		{
			if (!m_raycastUsesShape) {
				return true;
			}

			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var localPoint)) {
				return false;
			}

			if (!TryBuildShape(out var shape)) {
				return false;
			}

			// Raycast Padding can extend the hit area past the rect. Out there, a point counts when the nearest
			// point of the rect is inside the shape.
			var rect = rectTransform.rect;
			localPoint = new Vector2(Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax), Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax));

			if (SignedDistance(localPoint, shape) > RAYCAST_TOLERANCE) {
				return false;
			}

			return IsRevealed(localPoint - shape.Center, shape);
		}

		/// <summary>
		/// Signed distance from a local point to the shape edge (negative inside). The edge matches the shader;
		/// with a bulge or squircle corners the distance away from the edge is approximate.
		/// </summary>
		public float SignedDistance(Vector2 localPoint)
		{
			return TryBuildShape(out var shape) ? SignedDistance(localPoint, shape) : float.MaxValue;
		}

		private float SignedDistance(Vector2 localPoint, in ShapeData shape)
		{
			var p = localPoint - shape.Center;
			p = new Vector2(p.x / shape.InvScale.x, p.y / shape.InvScale.y);

			return ShapeDistanceField.Evaluate(p, shape.HalfSize, shape.Radii, shape.Bulge, GetCornerTypes());
		}

		/// <summary>The shape without its texture mapping, for hit tests.</summary>
		private bool TryBuildShape(out ShapeData shape)
		{
			if (!TryResolveShapeBounds(out var center, out var halfSize, out var bulge)) {
				shape = default;
				return false;
			}

			shape = BuildShapeData(center, halfSize, bulge, false);
			return true;
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			m_radius = Mathf.Max(0, m_radius);
			m_cornerRadii = Vector4.Max(m_cornerRadii, Vector4.zero);

			TrimList(m_outlines);
			TrimList(m_shadows);

			for (var i = 0; i < m_outlines.Count; i++) {
				var outline = m_outlines[i];
				outline.Width = Mathf.Max(0, outline.Width);
				m_outlines[i] = outline;
			}

			for (var i = 0; i < m_shadows.Count; i++) {
				var shadow = m_shadows[i];
				shadow.Softness = Mathf.Max(0, shadow.Softness);
				m_shadows[i] = shadow;
			}
		}

		private static void TrimList<T>(List<T> list)
		{
			if (list.Count > MAX_LAYERS_PER_LIST) {
				list.RemoveRange(MAX_LAYERS_PER_LIST, list.Count - MAX_LAYERS_PER_LIST);
			}
		}
#endif
	}

	/// <summary>
	/// C# copy of the shader's distance function, used for raycasts. Keep the two in sync.
	/// The shader also divides by the field's slope to even out distances along bulged sides and squircle corners;
	/// that never moves the edge, so it is left out here.
	/// </summary>
	public static class ShapeDistanceField
	{
		/// <param name="p">Point relative to the shape center.</param>
		/// <param name="radii">Top-left, top-right, bottom-right, bottom-left.</param>
		/// <param name="bulge">Top, right, bottom, left.</param>
		public static float Evaluate(Vector2 p, Vector2 halfSize, Vector4 radii, Vector4 bulge, CornerTypes cornerTypes)
		{
			p = ApplyBulge(p, halfSize, bulge);

			// Same corner split as the shader: midpoint between neighbouring corners' circle centers.
			var splitY = p.x >= 0f ? (radii.z - radii.y) * 0.5f : (radii.w - radii.x) * 0.5f;
			var top = p.y >= splitY;
			var splitX = top ? (radii.x - radii.y) * 0.5f : (radii.w - radii.z) * 0.5f;
			var right = p.x >= splitX;

			var radius = top ? (right ? radii.y : radii.x) : (right ? radii.z : radii.w);
			var cornerType = top
				? (right ? cornerTypes.TopRight : cornerTypes.TopLeft)
				: (right ? cornerTypes.BottomRight : cornerTypes.BottomLeft);

			var a = new Vector2((right ? p.x : -p.x) - halfSize.x, (top ? p.y : -p.y) - halfSize.y);
			return CornerDistance(a, radius, cornerType);
		}

		private static Vector2 ApplyBulge(Vector2 p, Vector2 halfSize, Vector4 bulge)
		{
			var halfX = Mathf.Max(halfSize.x, 1e-4f);
			var halfY = Mathf.Max(halfSize.y, 1e-4f);
			var fromCenterX = Mathf.Abs(p.x);
			var fromCenterY = Mathf.Abs(p.y);

			// Each side's push fades out close to the shape's own center line and never carries a point across it.
			var strengthX = (p.x >= 0f ? bulge.y : bulge.w) * Mathf.Clamp01(fromCenterX / (halfX * 0.1f));
			var strengthY = (p.y >= 0f ? bulge.x : bulge.z) * Mathf.Clamp01(fromCenterY / (halfY * 0.1f));

			var pushX = strengthX * Mathf.Cos(Mathf.PI * 0.5f * Mathf.Clamp01(fromCenterY / halfY));
			var pushY = strengthY * Mathf.Cos(Mathf.PI * 0.5f * Mathf.Clamp01(fromCenterX / halfX));

			return new Vector2(
				(p.x >= 0f ? 1f : -1f) * Mathf.Max(fromCenterX - pushX, 0f),
				(p.y >= 0f ? 1f : -1f) * Mathf.Max(fromCenterY - pushY, 0f));
		}

		private static float CornerDistance(Vector2 a, float radius, CornerType cornerType)
		{
			var boxDistance = new Vector2(Mathf.Max(a.x, 0), Mathf.Max(a.y, 0)).magnitude + Mathf.Min(Mathf.Max(a.x, a.y), 0);

			if (radius <= 0f) {
				return boxDistance;
			}

			switch (cornerType) {
				case CornerType.Chamfer:
					return Mathf.Max(boxDistance, (a.x + a.y + radius) * 0.70710678f);
				case CornerType.Inverted:
					return Mathf.Max(boxDistance, radius - a.magnitude);
			}

			var q = new Vector2(a.x + radius, a.y + radius);
			var inside = Mathf.Min(Mathf.Max(q.x, q.y), 0);
			var outside = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0));

			if (cornerType == CornerType.Squircle) {
				var o2 = new Vector2(outside.x * outside.x, outside.y * outside.y);
				return Mathf.Sqrt(Mathf.Sqrt(Vector2.Dot(o2, o2))) + inside - radius;
			}

			return outside.magnitude + inside - radius;
		}
	}
}
