using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui.Styled
{
	/// <summary>
	/// A resolution-independent UI rectangle drawn from a signed distance field: a shape (radius, per corner
	/// radii, pill, corner mask) and an ordered list of layers (fills, strokes, drop and inner shadows, bevels)
	/// where the first is in front.
	///
	/// Its values live in the StyledRectStore's data texture and its vertices carry only a position and two
	/// addresses into that texture, so the canvas needs no additional shader channels. Changing a value that
	/// does not move where a layer can draw (a color, a gradient, a radius) only writes the texture. Changing
	/// one that does (a stroke's width, the number of layers) rebuilds the mesh.
	///
	/// Each layer is its own quad, or a ring of quads around its empty middle, tinted by the Graphic color.
	/// An optional sprite is tiled across the shape and multiplies every layer that has Textured on.
	/// </summary>
	[AddComponentMenu("UI/Styled Rect")]
	[RequireComponent(typeof(CanvasRenderer))] // Like Image: Graphic only adds one lazily, too late for a fresh component.
	public class StyledRect : MaskableGraphic, ICanvasRaycastFilter
	{
		public StyledRadiusMode RadiusMode {
			get => m_radiusMode;
			set => SetShapeValue(ref m_radiusMode, value);
		}

		/// <summary>Radius used by StyledRadiusMode.Uniform.</summary>
		public float Radius {
			get => m_radius;
			set => SetShapeValue(ref m_radius, Mathf.Max(0, value));
		}

		/// <summary>Radii used by StyledRadiusMode.PerCorner: top-left, top-right, bottom-right, bottom-left.</summary>
		public Vector4 CornerRadii {
			get => m_cornerRadii;
			set => SetShapeValue(ref m_cornerRadii, Vector4.Max(value, Vector4.zero));
		}

		/// <summary>Corners that use the radius. The rest are square. Applies to every radius mode.</summary>
		public StyledCorners RoundedCorners {
			get => m_roundedCorners;
			set => SetShapeValue(ref m_roundedCorners, value);
		}

		/// <summary>
		/// How much the corners ease into the sides. 0 is a circular corner. 1 is a squircle, the corner of an
		/// app icon: fuller toward the corner and with no visible point where the curve meets the side.
		/// The corner keeps its radius, so it never reaches further along the side than a circular one.
		/// </summary>
		public float CornerSmoothing {
			get => m_cornerSmoothing;
			set => SetShapeValue(ref m_cornerSmoothing, Mathf.Clamp01(value));
		}

		/// <summary>
		/// How far the shape's edge wobbles in and out, for a soft, hand-made outline. Every layer follows it.
		/// 0 turns it off. A rect with a wobble draws with the Extended shader.
		/// </summary>
		public float WobbleAmount {
			get => m_wobbleAmount;
			set {
				value = Mathf.Max(0, value);

				if (m_wobbleAmount == value) {
					return;
				}

				// The wobble reaches past the shape, so the layers' quads change, and it can change the shader.
				m_wobbleAmount = value;
				SetVerticesDirty();
				SetMaterialDirty();
			}
		}

		/// <summary>Length of one lobe along the edge. Adjusted slightly so a whole number fits around the shape.</summary>
		public float WobbleLength {
			get => m_wobbleLength;
			set => SetTextureValue(ref m_wobbleLength, Mathf.Max(1, value));
		}

		/// <summary>Picks a different set of lobes.</summary>
		public float WobbleSeed {
			get => m_wobbleSeed;
			set => SetTextureValue(ref m_wobbleSeed, value);
		}

		/// <summary>
		/// How many times a second the lobes change into a new set. The edge morphs in place and does not
		/// travel around the shape. 0 keeps it still. Runs in the shader from game time.
		/// </summary>
		public float WobbleSpeed {
			get => m_wobbleSpeed;
			set => SetTextureValue(ref m_wobbleSpeed, value);
		}

		/// <summary>
		/// Shows only part of the rect, growing from a side or sweeping around the center. Layers with Ignore
		/// Reveal on stay whole.
		/// </summary>
		public StyledReveal Reveal {
			get => m_reveal;
			set => SetRevealValue(ref m_reveal, value);
		}

		/// <summary>How much of the rect shows: 0 hides it, 1 shows all of it. Never rebuilds the mesh.</summary>
		public float RevealAmount {
			get => m_revealAmount;
			set => SetTextureValue(ref m_revealAmount, Mathf.Clamp01(value));
		}

		/// <summary>Linear reveals only: whether the growing end has the rect's own shape or is cut straight.</summary>
		public StyledRevealCap RevealCap {
			get => m_revealCap;
			set => SetRevealValue(ref m_revealCap, value);
		}

		/// <summary>Radial reveals only: where the sweep starts.</summary>
		public StyledRadialStart RadialStart {
			get => m_radialStart;
			set => SetTextureValue(ref m_radialStart, value);
		}

		/// <summary>Radial reveals only: sweep clockwise from the start.</summary>
		public bool RadialClockwise {
			get => m_radialClockwise;
			set => SetTextureValue(ref m_radialClockwise, value);
		}

		/// <summary>Optional sprite. Multiplies every layer that has Textured on. See TextureMode for how it is placed.</summary>
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

		/// <summary>Cover fills the shape with the sprite and crops the overflow. Tile repeats it at its own size.</summary>
		public StyledTextureMode TextureMode {
			get => m_textureMode;
			set => SetTextureValue(ref m_textureMode, value);
		}

		/// <summary>Cover: zooms the sprite. Tile: multiplies the tile size.</summary>
		public Vector2 TextureScale {
			get => m_textureScale;
			set => SetTextureValue(ref m_textureScale, value);
		}

		/// <summary>Moves the texture, where 1 is one whole sprite or tile.</summary>
		public Vector2 TextureOffset {
			get => m_textureOffset;
			set => SetTextureValue(ref m_textureOffset, value);
		}

		/// <summary>Rotation of the texture in degrees, around the shape center.</summary>
		public float TextureRotation {
			get => m_textureRotation;
			set => SetTextureValue(ref m_textureRotation, value);
		}

		/// <summary>
		/// Tile mode only. Scroll speed in tiles per second along the texture's own axes. Runs in the shader from
		/// game time, so it costs nothing per frame and stops when Time.timeScale is 0.
		/// </summary>
		public Vector2 TextureScroll {
			get => m_textureScroll;
			set => SetTextureValue(ref m_textureScroll, value);
		}

		/// <summary>How the whole rect combines with what is behind it. Ignored when a custom Material is assigned.</summary>
		public StyledBlendMode BlendMode {
			get => m_blendMode;
			set {
				if (m_blendMode == value) {
					return;
				}

				m_blendMode = value;
				SetMaterialDirty();
			}
		}

		/// <summary>Ignore raycasts outside the shape, such as in the cut-away of a rounded corner, instead of using the full rect.</summary>
		public bool RaycastUsesShape {
			get => m_raycastUsesShape;
			set => m_raycastUsesShape = value;
		}

		/// <summary>Layers are ordered front to back: index 0 is drawn on top.</summary>
		public int LayerCount => m_layers.Count;

		public override Material defaultMaterial => StyledRectStore.Instance.GetMaterial(m_blendMode, UsesExtendedShader());

		public override Texture mainTexture => m_sprite != null ? m_sprite.texture : s_WhiteTexture;

		// Room for the anti-aliased edge, which is a screen pixel wide and so can be several units on a
		// scaled-down canvas.
		private const float EDGE_PADDING = 4f;

		// A stroke is drawn as a ring around its empty middle once the hole is at least this big on both sides
		// and covers this much of the layer's quad. Below that the extra quads cost more than the pixels saved.
		private const float MIN_RING_HOLE_SIZE = 16f;
		private const float MIN_RING_HOLE_FRACTION = 0.25f;

		// A soft edge fades like a Gaussian blur whose 4 standard deviations span its width. Its tails run
		// past that, and three deviations each way is where they stop being visible.
		private const float SOFT_REACH = 0.75f;

		// A corner is a superellipse: exponent 2 is a circle, 4 is the squircle at full smoothing.
		private const float ROUND_EXPONENT = 2f;
		private const float SQUIRCLE_EXPONENT = 4f;

		private const float RAYCAST_TOLERANCE = 0.01f;

		// A slide this far past the rect's size hides it entirely, with no sliver left where the two shapes meet.
		private const float HIDDEN_SLIDE_MARGIN = 4f;

		// Texture coordinate limits that never clamp anything, for a repeating texture.
		private const float NO_UV_LIMIT = 1e6f;

		[SerializeField] private StyledRadiusMode m_radiusMode = StyledRadiusMode.Uniform;
		[SerializeField] private float m_radius = 16;
		[SerializeField] private Vector4 m_cornerRadii = new Vector4(16, 16, 16, 16);

		[Tooltip("Corners that use the radius. Others are square.")]
		[SerializeField] private StyledCorners m_roundedCorners = StyledCorners.All;

		[Tooltip("How much the corners ease into the sides. 0 is a circular corner, 1 is a squircle like an app icon's.")]
		[SerializeField, Range(0, 1)] private float m_cornerSmoothing;

		[Tooltip("How far the shape's edge wobbles in and out, for a soft, hand-made outline. Every layer follows it. 0 turns it off.")]
		[SerializeField] private float m_wobbleAmount;

		[Tooltip("Length of one lobe along the edge. Adjusted slightly so a whole number fits around the shape.")]
		[SerializeField] private float m_wobbleLength = 80;

		[Tooltip("Picks a different set of lobes.")]
		[SerializeField] private float m_wobbleSeed;

		[Tooltip("How many times a second the lobes change into a new set. The edge morphs in place. 0 keeps it still. Uses game time.")]
		[SerializeField] private float m_wobbleSpeed;

		[Tooltip("Shows only part of the rect, for progress bars and cooldowns. Every layer follows the revealed shape unless it has Ignore Reveal on.")]
		[SerializeField] private StyledReveal m_reveal = StyledReveal.None;

		[Tooltip("How much of the rect shows: 0 hides it, 1 shows all of it.")]
		[SerializeField, Range(0, 1)] private float m_revealAmount = 1;

		[Tooltip("Whether the growing end has the rect's own shape, so a rounded bar keeps a rounded end, or is cut straight.")]
		[SerializeField] private StyledRevealCap m_revealCap = StyledRevealCap.Shape;

		[Tooltip("Where a radial reveal starts.")]
		[SerializeField] private StyledRadialStart m_radialStart = StyledRadialStart.Top;

		[Tooltip("Sweep clockwise from the start.")]
		[SerializeField] private bool m_radialClockwise = true;

		[Tooltip("Multiplies every layer that has Use Texture on.")]
		[SerializeField] private Sprite m_sprite;

		[Tooltip("Cover fills the shape with the sprite, keeps its aspect ratio and crops the overflow. Tile repeats it at its own size.")]
		[SerializeField] private StyledTextureMode m_textureMode = StyledTextureMode.Cover;

		[Tooltip("Cover: zooms the sprite. Tile: multiplies the tile size.")]
		[SerializeField] private Vector2 m_textureScale = Vector2.one;

		[Tooltip("Moves the texture, where 1 is one whole sprite or tile.")]
		[SerializeField] private Vector2 m_textureOffset;

		[Tooltip("Rotation of the texture in degrees, around the shape center.")]
		[SerializeField, Range(-180, 180)] private float m_textureRotation;

		[Tooltip("Tile mode only. Scroll speed in tiles per second, along the texture's own axes. Uses game time, so it stops when Time.timeScale is 0.")]
		[SerializeField] private Vector2 m_textureScroll;

		[Tooltip("How the whole rect combines with what is behind it. Each mode in use draws in its own batch. Ignored when a custom Material is assigned.")]
		[SerializeField] private StyledBlendMode m_blendMode = StyledBlendMode.Normal;

		[Tooltip("Ignore raycasts outside the shape, such as in the cut-away of a rounded corner, instead of using the full rect.")]
		[SerializeField] private bool m_raycastUsesShape = true;

		[Tooltip("Front to back: the first layer is drawn on top.")]
		[SerializeField] private List<StyledLayer> m_layers = new List<StyledLayer> { StyledLayer.Fill(Color.white) };

		private StyledRectStore m_store;
		private int m_address;
		private int m_texelCount;

		public StyledLayer GetLayer(int index) => m_layers[index];

		/// <summary>
		/// Replaces a layer. Only rebuilds the mesh when the new layer covers different pixels than the old one.
		/// </summary>
		public void SetLayer(int index, StyledLayer layer)
		{
			var previous = m_layers[index];
			m_layers[index] = layer;

			if (StyledLayer.SameGeometry(previous, layer)) {
				WriteRows();
			} else {
				SetVerticesDirty();
			}

			// Dashes or a wobbling width move the rect to the Extended shader.
			if (previous.UsesEdgePosition != layer.UsesEdgePosition) {
				SetMaterialDirty();
			}
		}

		/// <summary>Changes a layer's paint. Never rebuilds the mesh.</summary>
		public void SetLayerPaint(int index, StyledPaint paint)
		{
			var layer = m_layers[index];
			layer.Paint = paint;
			m_layers[index] = layer;
			WriteRows();
		}

		/// <summary>Adds a layer in front of the others.</summary>
		public void AddLayer(StyledLayer layer)
		{
			InsertLayer(0, layer);
		}

		public void InsertLayer(int index, StyledLayer layer)
		{
			m_layers.Insert(index, layer);
			SetVerticesDirty();
			SetMaterialDirty();
		}

		public void RemoveLayer(int index)
		{
			m_layers.RemoveAt(index);
			SetVerticesDirty();
			SetMaterialDirty();
		}

		/// <summary>
		/// Paints the rect's body: the frontmost fill that reaches the shape's edge and has no blur. A fill that
		/// is inset or blurred is a gloss or a highlight and is left alone. Adds a fill behind everything when
		/// there is no body.
		/// </summary>
		public void SetFill(StyledPaint paint)
		{
			for (var i = 0; i < m_layers.Count; i++) {
				var layer = m_layers[i];

				if (layer.Kind == StyledLayerKind.Fill && layer.Inset <= 0f && layer.Blur <= 0f) {
					SetLayerPaint(i, paint);
					return;
				}
			}

			InsertLayer(m_layers.Count, StyledLayer.Fill(paint));
		}

		/// <summary>Adds a stroke in front of the other layers.</summary>
		public void AddStroke(float width, StyledPaint paint, StyledStrokeAlign align = StyledStrokeAlign.Inside)
		{
			AddLayer(StyledLayer.Stroke(width, paint, align));
		}

		/// <summary>Adds a drop shadow behind every other layer.</summary>
		/// <param name="offset">With y up: (0, -6) is a shadow below the shape.</param>
		public void AddDropShadow(Vector2 offset, float blur, StyledPaint paint, float spread = 0)
		{
			InsertLayer(m_layers.Count, StyledLayer.DropShadow(offset, blur, paint, spread));
		}

		/// <summary>Adds an inner shadow just in front of the frontmost fill, so strokes stay on top of it.</summary>
		/// <param name="offset">With y up: (0, -4) shades the top inside edge.</param>
		public void AddInnerShadow(Vector2 offset, float blur, StyledPaint paint, float spread = 0)
		{
			InsertLayer(FrontmostFillIndex(), StyledLayer.InnerShadow(offset, blur, paint, spread));
		}

		/// <summary>Index of the frontmost fill, or the back of the list when there is none.</summary>
		private int FrontmostFillIndex()
		{
			for (var i = 0; i < m_layers.Count; i++) {
				if (m_layers[i].Kind == StyledLayerKind.Fill) {
					return i;
				}
			}

			return m_layers.Count;
		}

		/// <summary>
		/// Adds a glossy highlight just in front of the frontmost fill: a fill pulled in from the edge that fades
		/// out downward. See StyledLayer.Gloss.
		/// </summary>
		public void AddGloss(Color color, float inset, float blur, float height = 0.5f)
		{
			InsertLayer(FrontmostFillIndex(), StyledLayer.Gloss(color, inset, blur, height));
		}

		/// <summary>Adds a bevel just in front of the frontmost fill, so strokes stay on top of it.</summary>
		/// <param name="lightAngle">Where the light comes from. As in CSS: 0 is straight above and it turns clockwise.</param>
		public void AddBevel(float width, float blur, Color highlight, Color shadow, float lightAngle = 315)
		{
			InsertLayer(FrontmostFillIndex(), StyledLayer.Bevel(width, blur, highlight, shadow, lightAngle));
		}

		/// <summary>
		/// Whether a screen point hits the rect. With Raycast Uses Shape on, only points inside the shape count.
		/// Layers that reach past the shape, such as an outside stroke or a shadow, are not part of it.
		/// </summary>
		public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
		{
			if (!m_raycastUsesShape) {
				return true;
			}

			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var localPoint)) {
				return false;
			}

			if (!TryGetHalfSize(out var halfSize)) {
				return false;
			}

			// Raycast Padding can extend the hit area past the rect. Out there, a point counts when the nearest
			// point of the rect is inside the shape.
			var rect = GetPixelAdjustedRect();
			localPoint = new Vector2(Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax), Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax));

			var point = localPoint - rect.center;
			var radii = CalculateRadii(halfSize);

			return StyledShapeField.Distance(point, halfSize, radii, CornerExponent()) <= RAYCAST_TOLERANCE
			       && IsRevealed(point, halfSize, radii);
		}

		/// <summary>Whether a point relative to the shape center is in the revealed part. Matches the shader.</summary>
		private bool IsRevealed(Vector2 point, Vector2 halfSize, Vector4 radii)
		{
			var reveal = GetRevealRow(halfSize);

			switch ((int)reveal.x) {
				case StyledRows.REVEAL_NONE:
					return true;
				case StyledRows.REVEAL_SLIDE:
					return StyledShapeField.Distance(point - new Vector2(reveal.y, reveal.z), halfSize, radii, CornerExponent()) <= RAYCAST_TOLERANCE;
				case StyledRows.REVEAL_STRAIGHT:
					return Vector2.Dot(point, new Vector2(reveal.y, reveal.z)) <= reveal.w + RAYCAST_TOLERANCE;
				case StyledRows.REVEAL_RADIAL: {
					// Angle swept from the start toward the point, in the direction of the sweep.
					var sweep = reveal.z;
					var angle = Mathf.Atan2(point.y, point.x);
					var swept = Mathf.Repeat((angle - reveal.y) * Mathf.Sign(sweep), Mathf.PI * 2f);
					return swept <= Mathf.Abs(sweep);
				}
				default:
					return false;
			}
		}

		protected override void OnEnable()
		{
			EnsureRows();
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			ReleaseRows();
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			if (!TryGetHalfSize(out var halfSize)) {
				return;
			}

			EnsureRows();
			WriteRows();

			var center = GetPixelAdjustedRect().center;
			var radii = CalculateRadii(halfSize);

			// Back to front, so the first layer ends up on top.
			for (var i = m_layers.Count - 1; i >= 0; i--) {
				var layer = m_layers[i];

				if (!layer.Draws) {
					continue;
				}

				GetLayerBounds(layer, halfSize, radii, out var min, out var max, out var holeMin, out var holeMax);

				// A wobble moves edges either way, so the layer reaches further out and its hole is smaller.
				var wobble = Vector2.one * (Mathf.Max(0, m_wobbleAmount) + layer.WobbleReach);
				min -= wobble;
				max += wobble;
				holeMin += wobble;
				holeMax -= wobble;

				// A hole can reach past a layer that is offset from the shape.
				holeMin = Vector2.Max(holeMin, min);
				holeMax = Vector2.Min(holeMax, max);

				var layerAddress = StyledRows.LayerAddress(m_address, i);

				if (AllowsHole(layer) && IsHoleWorthIt(min, max, holeMin, holeMax)) {
					EmitRing(vh, center, min, max, holeMin, holeMax, layerAddress);
				} else {
					EmitRect(vh, center, min, max, layerAddress);
				}
			}
		}

		/// <summary>
		/// Where a layer can draw, as a rect relative to the shape center, and the rect in its middle where it
		/// draws nothing (empty when there is none). Drawing around the hole means the GPU never shades it.
		/// </summary>
		private static void GetLayerBounds(in StyledLayer layer, Vector2 halfSize, Vector4 radii,
			out Vector2 min, out Vector2 max, out Vector2 holeMin, out Vector2 holeMax)
		{
			holeMin = holeMax = Vector2.zero;

			switch (layer.Kind) {
				case StyledLayerKind.Shadow: {
					var reach = halfSize + Vector2.one * (Mathf.Max(layer.Spread, 0) + layer.SoftWidth * SOFT_REACH + EDGE_PADDING);
					min = layer.ShadowOffset - reach;
					max = layer.ShadowOffset + reach;

					// The shadow is cut out under the shape, so nothing is drawn well inside it.
					if (!layer.ShowBehind) {
						TryGetInnerRect(halfSize, radii, EDGE_PADDING, out holeMin, out holeMax);
					}

					break;
				}
				case StyledLayerKind.InnerShadow: {
					var reach = halfSize + Vector2.one * EDGE_PADDING;
					min = -reach;
					max = reach;

					// Shadowed where the offset shape, pushed in by the spread, is not. Deep inside that there is none.
					var depth = Mathf.Max(layer.Spread, 0) + layer.SoftWidth * SOFT_REACH + EDGE_PADDING;

					if (TryGetInnerRect(halfSize, SoftRadii(layer, halfSize, radii), depth, out holeMin, out holeMax)) {
						holeMin += layer.ShadowOffset;
						holeMax += layer.ShadowOffset;
					}

					break;
				}
				case StyledLayerKind.Bevel: {
					var reach = halfSize + Vector2.one * EDGE_PADDING;
					min = -reach;
					max = reach;

					// Nothing is drawn deeper than the bevel's width and its soft inner edge.
					TryGetInnerRect(halfSize, radii, Mathf.Max(0, layer.Width) + layer.SoftWidth * SOFT_REACH + EDGE_PADDING, out holeMin, out holeMax);
					break;
				}
				default: {
					var band = layer.GetBand();
					var reach = halfSize + Vector2.one * (Mathf.Max(band.y, 0) + layer.SoftWidth * SOFT_REACH + EDGE_PADDING);
					min = -reach;
					max = reach;

					// A stroke leaves the middle of the shape empty. Stay clear of its anti-aliased inner edge.
					if (band.x > StyledLayer.NO_INNER_EDGE) {
						TryGetInnerRect(halfSize, radii, Mathf.Max(0f, -band.x + EDGE_PADDING), out holeMin, out holeMax);
					}

					break;
				}
			}
		}

		/// <summary>
		/// The corner radii a layer's soft part measures against. Deeper than a corner's radius the distance
		/// field turns that corner into a sharp mitre, which anything soft that reaches that deep (an inner
		/// shadow, an Edge gradient) would show as a diagonal crease. A blur rounds its corners instead, so
		/// those layers measure against corners at least as round as they are deep.
		/// </summary>
		private static Vector4 SoftRadii(in StyledLayer layer, Vector2 halfSize, Vector4 radii)
		{
			return Vector4.Max(radii, Vector4.one * SoftRounding(layer, halfSize, radii));
		}

		/// <summary>
		/// The least radius a layer's soft radii have: they are the corner radii raised to at least this.
		/// 0 for a layer with nothing soft, which measures against the corner radii as they are. This one
		/// value is all the shader is given.
		/// </summary>
		private static float SoftRounding(in StyledLayer layer, Vector2 halfSize, Vector4 radii)
		{
			var smallest = Mathf.Min(halfSize.x, halfSize.y);
			var depth = 0f;

			switch (layer.Kind) {
				case StyledLayerKind.InnerShadow:
					depth = Mathf.Max(layer.Spread, 0) + layer.SoftWidth * 0.5f;
					break;
				case StyledLayerKind.Fill:
					// A blurred fill is soft as deep as its inset plus half its soft edge.
					if (layer.SoftWidth > 0) {
						depth = Mathf.Max(layer.Inset, 0) + layer.SoftWidth * 0.5f;
					}

					// An Edge gradient only as deep as it still changes. Past its range end it is flat.
					if (layer.Paint.Gradient == StyledGradient.Edge) {
						depth = Mathf.Max(depth, smallest * Mathf.Clamp01(layer.Paint.RangeEnd));
					}

					break;
				case StyledLayerKind.Stroke:
					if (layer.Paint.Gradient == StyledGradient.Edge) {
						depth = Mathf.Max(-layer.GetBand().x, 0);
					}

					break;
			}

			if (depth <= 0) {
				return 0f;
			}

			var largest = Mathf.Max(Mathf.Max(radii.x, radii.y), Mathf.Max(radii.z, radii.w));

			return Mathf.Max(0, Mathf.Min(depth, Mathf.Min(smallest, 2f * smallest - largest)));
		}

		/// <summary>
		/// Whether a layer's hole is big enough to draw the layer as a ring around it. Below this the extra quads
		/// cost more than the pixels saved.
		/// </summary>
		private static bool IsHoleWorthIt(Vector2 min, Vector2 max, Vector2 holeMin, Vector2 holeMax)
		{
			var holeSize = holeMax - holeMin;
			var quadSize = max - min;

			return holeSize.x >= MIN_RING_HOLE_SIZE && holeSize.y >= MIN_RING_HOLE_SIZE
			       && holeSize.x * holeSize.y >= quadSize.x * quadSize.y * MIN_RING_HOLE_FRACTION;
		}

		/// <summary>
		/// The rect (relative to the shape center) whose every point is at least `depth` inside the shape.
		/// </summary>
		private static bool TryGetInnerRect(Vector2 halfSize, Vector4 radii, float depth, out Vector2 min, out Vector2 max)
		{
			var topLeft = CornerInset(radii.x, depth);
			var topRight = CornerInset(radii.y, depth);
			var bottomRight = CornerInset(radii.z, depth);
			var bottomLeft = CornerInset(radii.w, depth);

			min = new Vector2(-halfSize.x + Mathf.Max(bottomLeft, topLeft), -halfSize.y + Mathf.Max(bottomRight, bottomLeft));
			max = new Vector2(halfSize.x - Mathf.Max(topRight, bottomRight), halfSize.y - Mathf.Max(topLeft, topRight));

			if (max.x > min.x && max.y > min.y) {
				return true;
			}

			min = max = Vector2.zero;
			return false;
		}

		/// <summary>
		/// How far in from both of a round corner's sides a point must be to sit `depth` inside the shape there.
		/// Solved from the corner's distance function at the point (-k, -k) from the corner.
		/// A smoothed corner is fuller than a circular one, so this bound is safe for it too.
		/// </summary>
		private static float CornerInset(float radius, float depth)
		{
			const float invSqrt2 = 0.70710678f;
			return Mathf.Max(depth, (1f - invSqrt2) * radius + invSqrt2 * depth);
		}

		/// <summary>Draws a layer as four rects around a hole: full-width bottom and top, then left and right between them.</summary>
		private void EmitRing(VertexHelper vh, Vector2 center, Vector2 min, Vector2 max, Vector2 holeMin, Vector2 holeMax, int layerAddress)
		{
			EmitRect(vh, center, min, new Vector2(max.x, holeMin.y), layerAddress);
			EmitRect(vh, center, new Vector2(min.x, holeMax.y), max, layerAddress);
			EmitRect(vh, center, new Vector2(min.x, holeMin.y), new Vector2(holeMin.x, holeMax.y), layerAddress);
			EmitRect(vh, center, new Vector2(holeMax.x, holeMin.y), new Vector2(max.x, holeMax.y), layerAddress);
		}

		/// <summary>
		/// Every piece of geometry goes through here: a rect in the shape's own space, relative to its center.
		/// Shape deformation will later subdivide and displace the positions at this point, leaving the shape
		/// space coordinates in uv0 untouched.
		/// </summary>
		private void EmitRect(VertexHelper vh, Vector2 center, Vector2 min, Vector2 max, int layerAddress)
		{
			if (max.x - min.x <= 0.0001f || max.y - min.y <= 0.0001f) {
				return;
			}

			var startIndex = vh.currentVertCount;

			AddVertex(vh, center, min, layerAddress);
			AddVertex(vh, center, new Vector2(min.x, max.y), layerAddress);
			AddVertex(vh, center, max, layerAddress);
			AddVertex(vh, center, new Vector2(max.x, min.y), layerAddress);

			vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
			vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		private void AddVertex(VertexHelper vh, Vector2 center, Vector2 local, int layerAddress)
		{
			var vertex = UIVertex.simpleVert;
			vertex.position = center + local;
			vertex.color = color;
			vertex.uv0 = new Vector4(local.x, local.y, m_address, layerAddress);
			vh.AddVert(vertex);
		}

		/// <summary>
		/// Sets a shape field and writes the rect's texels, but only when the value changed. No mesh rebuild.
		/// </summary>
		private void SetShapeValue<T>(ref T field, T value)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) {
				return;
			}

			field = value;

			// Holes are cut around the corners, so a rect with a holed layer needs its mesh rebuilt when they change.
			if (HasHoledLayer()) {
				SetVerticesDirty();
			} else {
				WriteRows();
			}
		}

		/// <summary>
		/// Sets a texture field and writes the rect's texels, but only when the value changed. Never a mesh rebuild.
		/// </summary>
		private void SetTextureValue<T>(ref T field, T value)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) {
				return;
			}

			field = value;
			WriteRows();
		}

		/// <summary>
		/// Sets a field that picks the kind of reveal. A slide reveal draws its layers without holes, so the
		/// mesh is rebuilt when the rect moves into or out of one. Anything else only writes the rect's texels.
		/// </summary>
		private void SetRevealValue<T>(ref T field, T value)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) {
				return;
			}

			var wasSlide = IsSlideReveal();
			field = value;

			if (wasSlide != IsSlideReveal() && HasHoledLayer()) {
				SetVerticesDirty();
			} else {
				WriteRows();
			}
		}

		/// <summary>
		/// Whether the reveal is a slide: a linear reveal with a Shape cap, which moves the shape's edge into
		/// the middle of the rect. It does not look at the amount, so changing the amount never rebuilds the mesh.
		/// </summary>
		private bool IsSlideReveal()
		{
			return m_reveal != StyledReveal.None && m_reveal != StyledReveal.Radial && m_revealCap == StyledRevealCap.Shape;
		}

		/// <summary>
		/// Whether a layer may be drawn as a ring around its empty middle. Holes are cut from the whole shape.
		/// Under a slide reveal the growing end's stroke, bevel and shadows run across that middle, so a layer
		/// that follows the reveal is drawn whole.
		/// </summary>
		private bool AllowsHole(in StyledLayer layer)
		{
			return layer.IgnoreReveal || !IsSlideReveal();
		}

		/// <summary>
		/// Whether the rect needs the Extended shader: its shape wobbles, or a stroke is dashed or wobbles.
		/// Everything else draws with the Core shader.
		/// </summary>
		private bool UsesExtendedShader()
		{
			if (m_wobbleAmount > 0) {
				return true;
			}

			foreach (var layer in m_layers) {
				if (layer.UsesEdgePosition) {
					return true;
				}
			}

			return false;
		}

		private bool HasHoledLayer()
		{
			foreach (var layer in m_layers) {
				if (layer.Draws && layer.Kind != StyledLayerKind.Fill) {
					return true;
				}
			}

			return false;
		}

		private bool TryGetHalfSize(out Vector2 halfSize)
		{
			var rect = GetPixelAdjustedRect();
			halfSize = new Vector2(Mathf.Abs(rect.width), Mathf.Abs(rect.height)) * 0.5f;

			return halfSize.x > 0 && halfSize.y > 0;
		}

		/// <summary>
		/// Makes sure this rect has texels in the store, enough for its layers. The store is replaced on a
		/// domain reload, and an address from the old one means nothing in the new one.
		/// </summary>
		private void EnsureRows()
		{
			var store = StyledRectStore.Instance;
			var texelCount = StyledRows.SpanTexels(m_layers.Count);

			if (m_store == store && m_texelCount == texelCount) {
				return;
			}

			ReleaseRows();

			m_store = store;
			m_texelCount = texelCount;
			m_address = store.Allocate(texelCount);
			WriteRows();
		}

		private void ReleaseRows()
		{
			if (m_store == null) {
				return;
			}

			m_store.Free(m_address, m_texelCount);
			m_store = null;
			m_texelCount = 0;
		}

		private void WriteRows()
		{
			// The layer count can change before the mesh rebuild that gives the rect its new texels.
			if (m_store == null || m_texelCount != StyledRows.SpanTexels(m_layers.Count) || !TryGetHalfSize(out var halfSize)) {
				return;
			}

			var radii = CalculateRadii(halfSize);

			m_store.Write(m_address + StyledRows.SHAPE_SIZE, new Vector4(halfSize.x, halfSize.y, 0, 0));
			m_store.Write(m_address + StyledRows.SHAPE_RADII, radii);
			m_store.Write(m_address + StyledRows.SHAPE_CORNER_EXPONENTS, Vector4.one * CornerExponent());
			m_store.Write(m_address + StyledRows.SHAPE_REVEAL, GetRevealRow(halfSize));
			m_store.Write(m_address + StyledRows.SHAPE_WOBBLE, new Vector4(Mathf.Max(0, m_wobbleAmount), Mathf.Max(1, m_wobbleLength), m_wobbleSeed, m_wobbleSpeed));
			WriteTextureRows(halfSize);

			// Front to back. A fill under an opaque layer that sits on the shape's edge ends a pixel early.
			// Both edges are anti-aliased in the same place, and without this the fill would show through
			// the layer above as a fringe. A layer that follows the reveal only covers the edge where it is
			// revealed, so it does not cover for a fill behind it that ignores the reveal.
			var revealing = m_reveal != StyledReveal.None;
			var coveredEverywhere = false;
			var coveredWhereRevealed = false;

			for (var i = 0; i < m_layers.Count; i++) {
				var layer = m_layers[i];
				var band = layer.GetBand();
				var underlay = layer.Kind == StyledLayerKind.Fill
				               && (coveredEverywhere || (coveredWhereRevealed && !layer.IgnoreReveal));
				var flags = (underlay ? StyledRows.FLAG_UNDERLAY : 0)
				            | (layer.ShowBehind ? StyledRows.FLAG_SHOW_BEHIND : 0)
				            | (IsTextured(layer) ? StyledRows.FLAG_TEXTURED : 0)
				            | (layer.IgnoreTint ? StyledRows.FLAG_IGNORE_TINT : 0)
				            | (layer.IgnoreReveal ? StyledRows.FLAG_IGNORE_REVEAL : 0);
				var address = StyledRows.LayerAddress(m_address, i);

				m_store.Write(address + StyledRows.LAYER_HEADER,
					new Vector4((int)layer.Kind + flags * StyledRows.FLAG_SCALE, SoftRounding(layer, halfSize, radii), band.x, band.y));
				m_store.Write(address + StyledRows.LAYER_SHADOW, new Vector4(layer.ShadowOffset.x, layer.ShadowOffset.y, layer.SoftWidth, layer.Spread));
				m_store.Write(address + StyledRows.LAYER_COLOR_A, layer.Paint.Color);
				// A bevel always uses both colors: its highlight and its shadow.
				var usesColor2 = layer.Kind == StyledLayerKind.Bevel || layer.Paint.Gradient != StyledGradient.None;
				var rangeStart = Mathf.Clamp01(layer.Paint.RangeStart);
				var rangeEnd = Mathf.Clamp01(layer.Paint.RangeEnd);

				m_store.Write(address + StyledRows.LAYER_COLOR_B, usesColor2 ? layer.Paint.Color2 : layer.Paint.Color);
				m_store.Write(address + StyledRows.LAYER_GRADIENT, new Vector4((int)layer.Paint.Gradient, layer.Paint.Angle * Mathf.Deg2Rad,
					Mathf.Min(rangeStart, rangeEnd), Mathf.Max(rangeStart, rangeEnd)));
				m_store.Write(address + StyledRows.LAYER_CURVE_DASHES, new Vector4(layer.Paint.BiasExponent, Mathf.Max(0, layer.StrokeStyle.DashSpacing),
					Mathf.Clamp01(layer.StrokeStyle.DashLength) + (layer.StrokeStyle.DashRoundCaps ? 2f : 0f), layer.StrokeStyle.DashSpeed));
				m_store.Write(address + StyledRows.LAYER_STROKE_WOBBLE, new Vector4(Mathf.Max(0, layer.StrokeStyle.WobbleWidth), Mathf.Max(0, layer.StrokeStyle.WobbleDrift),
					Mathf.Max(0, layer.StrokeStyle.WobbleLength), layer.StrokeStyle.WobbleSeed));

				if (CoversEdge(layer, band)) {
					if (revealing && !layer.IgnoreReveal) {
						coveredWhereRevealed = true;
					} else {
						coveredEverywhere = true;
					}
				}
			}
		}

		/// <summary>
		/// The reveal as the shader and the hit test read it: a mode, then that mode's values.
		/// A linear reveal with a Shape cap is a slide: the visible part is where the shape overlaps a copy of
		/// itself moved back by the hidden distance, which is what gives the growing end the shape's own corners.
		/// </summary>
		private Vector4 GetRevealRow(Vector2 halfSize)
		{
			if (m_reveal == StyledReveal.None || m_revealAmount >= 1f) {
				return new Vector4(StyledRows.REVEAL_NONE, 0, 0, 0);
			}

			if (m_revealAmount <= 0f) {
				return new Vector4(StyledRows.REVEAL_HIDDEN, 0, 0, 0);
			}

			if (m_reveal == StyledReveal.Radial) {
				var start = m_radialStart switch {
					StyledRadialStart.Top => Mathf.PI * 0.5f,
					StyledRadialStart.Right => 0f,
					StyledRadialStart.Bottom => Mathf.PI * 1.5f,
					_ => Mathf.PI
				};

				// Angles grow counter-clockwise, so a clockwise sweep is negative.
				var sweep = m_revealAmount * Mathf.PI * 2f * (m_radialClockwise ? -1f : 1f);
				return new Vector4(StyledRows.REVEAL_RADIAL, start, sweep, 0);
			}

			// The direction the reveal grows in, and the rect's size along it.
			var direction = m_reveal switch {
				StyledReveal.FromLeft => Vector2.right,
				StyledReveal.FromRight => Vector2.left,
				StyledReveal.FromBottom => Vector2.up,
				_ => Vector2.down
			};

			var size = 2f * (direction.x != 0f ? halfSize.x : halfSize.y);

			if (m_revealCap == StyledRevealCap.Straight) {
				// Revealed where the position along the direction is at most the cut.
				return new Vector4(StyledRows.REVEAL_STRAIGHT, direction.x, direction.y, size * (m_revealAmount - 0.5f));
			}

			var slide = -direction * (size * (1f - m_revealAmount));
			return new Vector4(StyledRows.REVEAL_SLIDE, slide.x, slide.y, 0);
		}

		/// <summary>
		/// How shape space positions map to texture coordinates. The sprite sits on the shape center, either
		/// sized to cover the shape or repeated at its own size, then scaled, rotated and offset. The mapping
		/// lands in the sprite's own part of its texture, so a Cover sprite can be in an atlas.
		/// </summary>
		private void WriteTextureRows(Vector2 halfSize)
		{
			if (m_sprite == null) {
				return;
			}

			Vector2 size;
			Vector2 uvMin;
			Vector2 uvSize;
			Vector2 scroll;
			Vector4 limits;

			if (m_textureMode == StyledTextureMode.Tile) {
				var referencePixelsPerUnit = canvas != null ? canvas.referencePixelsPerUnit : 100f;
				size = m_sprite.rect.size * (referencePixelsPerUnit / Mathf.Max(0.0001f, m_sprite.pixelsPerUnit));
				uvMin = Vector2.zero;
				uvSize = Vector2.one;
				scroll = m_textureScroll;

				// A repeating texture has no limits.
				limits = new Vector4(-NO_UV_LIMIT, -NO_UV_LIMIT, NO_UV_LIMIT, NO_UV_LIMIT);
			} else {
				size = CoverSize(halfSize * 2f, m_sprite.rect.width / Mathf.Max(0.0001f, m_sprite.rect.height));

				var outer = UnityEngine.Sprites.DataUtility.GetOuterUV(m_sprite);
				uvMin = new Vector2(outer.x, outer.y);
				uvSize = new Vector2(outer.z - outer.x, outer.w - outer.y);
				scroll = Vector2.zero;

				// Stop half a texel inside the sprite, so filtering never reaches a neighbour in the atlas.
				var texture = m_sprite.texture;
				var halfTexel = new Vector2(0.5f / Mathf.Max(1, texture.width), 0.5f / Mathf.Max(1, texture.height));
				limits = new Vector4(outer.x + halfTexel.x, outer.y + halfTexel.y, outer.z - halfTexel.x, outer.w - halfTexel.y);
			}

			var divisor = new Vector2(NonZero(size.x * m_textureScale.x), NonZero(size.y * m_textureScale.y));

			// Rotate the opposite way so the texture appears rotated by the angle.
			var radians = m_textureRotation * Mathf.Deg2Rad;
			var cos = Mathf.Cos(radians);
			var sin = Mathf.Sin(radians);

			m_store.Write(m_address + StyledRows.SHAPE_TEXTURE_MATRIX,
				new Vector4(cos / divisor.x * uvSize.x, sin / divisor.x * uvSize.x, -sin / divisor.y * uvSize.y, cos / divisor.y * uvSize.y));
			m_store.Write(m_address + StyledRows.SHAPE_TEXTURE_OFFSET,
				new Vector4(uvMin.x + uvSize.x * (0.5f + m_textureOffset.x), uvMin.y + uvSize.y * (0.5f + m_textureOffset.y), scroll.x, scroll.y));
			m_store.Write(m_address + StyledRows.SHAPE_TEXTURE_LIMITS, limits);
		}

		/// <summary>The size a sprite of the given aspect ratio needs to cover a shape: it matches one side and overflows the other.</summary>
		private static Vector2 CoverSize(Vector2 shapeSize, float spriteAspect)
		{
			var shapeAspect = shapeSize.x / Mathf.Max(shapeSize.y, 0.0001f);

			return shapeAspect > spriteAspect
				? new Vector2(shapeSize.x, shapeSize.x / spriteAspect)
				: new Vector2(shapeSize.y * spriteAspect, shapeSize.y);
		}

		private static float NonZero(float value)
		{
			return Mathf.Abs(value) < 0.0001f ? (value < 0 ? -0.0001f : 0.0001f) : value;
		}

		/// <summary>Whether a layer samples the sprite. Bevels never do.</summary>
		private bool IsTextured(in StyledLayer layer)
		{
			return layer.Textured && m_sprite != null && layer.Kind != StyledLayerKind.Bevel;
		}

		/// <summary>
		/// Whether a layer hides the shape's edge from the fills behind it: an opaque solid stroke that sits on
		/// the edge and reaches at least a unit inside it, or an opaque fill out to the edge with no blur.
		/// A textured layer never counts, since the sprite can be transparent. Nor does a dashed or wobbling
		/// stroke, which leaves parts of the edge open.
		/// </summary>
		private bool CoversEdge(in StyledLayer layer, Vector2 band)
		{
			if (!layer.Draws || color.a < 1f || !layer.Paint.IsOpaque || IsTextured(layer) || band.y < 0f) {
				return false;
			}

			return layer.Kind switch {
				StyledLayerKind.Stroke => band.x <= -1f && !layer.UsesEdgePosition,
				StyledLayerKind.Fill => layer.SoftWidth <= 0f,
				_ => false
			};
		}

		/// <summary>The superellipse exponent of the corners: 2 with no smoothing, 4 at full smoothing.</summary>
		private float CornerExponent()
		{
			return Mathf.Lerp(ROUND_EXPONENT, SQUIRCLE_EXPONENT, Mathf.Clamp01(m_cornerSmoothing));
		}

		/// <summary>The radii the shader uses (top-left, top-right, bottom-right, bottom-left): masked, then fitted to the size.</summary>
		private Vector4 CalculateRadii(Vector2 halfSize)
		{
			var radii = m_radiusMode switch {
				StyledRadiusMode.Pill => Vector4.one * Mathf.Min(halfSize.x, halfSize.y),
				StyledRadiusMode.PerCorner => m_cornerRadii,
				_ => Vector4.one * m_radius
			};

			radii = MaskRadii(radii, m_roundedCorners);

			return FitRadii(radii, halfSize * 2f);
		}

		private static Vector4 MaskRadii(Vector4 radii, StyledCorners corners)
		{
			return new Vector4(
				(corners & StyledCorners.TopLeft) != 0 ? radii.x : 0,
				(corners & StyledCorners.TopRight) != 0 ? radii.y : 0,
				(corners & StyledCorners.BottomRight) != 0 ? radii.z : 0,
				(corners & StyledCorners.BottomLeft) != 0 ? radii.w : 0);
		}

		/// <summary>Scales the radii down together so that corners on the same side never overlap.</summary>
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

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			m_radius = Mathf.Max(0, m_radius);
			m_cornerRadii = Vector4.Max(m_cornerRadii, Vector4.zero);
			m_wobbleAmount = Mathf.Max(0, m_wobbleAmount);
			m_wobbleLength = Mathf.Max(1, m_wobbleLength);

			for (var i = 0; i < m_layers.Count; i++) {
				var layer = m_layers[i];
				layer.Width = Mathf.Max(0, layer.Width);
				layer.Blur = Mathf.Max(0, layer.Blur);
				layer.StrokeStyle.DashSpacing = Mathf.Max(0, layer.StrokeStyle.DashSpacing);
				layer.StrokeStyle.WobbleWidth = Mathf.Max(0, layer.StrokeStyle.WobbleWidth);
				layer.StrokeStyle.WobbleDrift = Mathf.Max(0, layer.StrokeStyle.WobbleDrift);
				layer.StrokeStyle.WobbleLength = Mathf.Max(0, layer.StrokeStyle.WobbleLength);
				m_layers[i] = layer;
			}

			base.OnValidate();
		}
#endif
	}
}
