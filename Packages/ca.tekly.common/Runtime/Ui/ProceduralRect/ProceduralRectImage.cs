using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Tekly.Common.Ui.ProceduralRect
{
	public enum ModifierType
	{
		Round,
		Uniform,
		OneEdge,
		Free
	}

	public enum ProceduralRectEdge
	{
		Top,
		Bottom,
		Left,
		Right
	}

	[AddComponentMenu("UI/Procedural Rect")]
	public class ProceduralRectImage : Image
	{
		public const AdditionalCanvasShaderChannels NEEDED_SHADER_CHANNELS = AdditionalCanvasShaderChannels.TexCoord1 |
		                                                                     AdditionalCanvasShaderChannels.TexCoord2 |
		                                                                     AdditionalCanvasShaderChannels.TexCoord3;

		public const float MIN_FALLOFF_POWER = 0.25f;
		public const float MAX_FALLOFF_POWER = 4f;

		private const float MAX_AA_SCALE = 2048f;

		// Two 12-bit values are packed into one float as an integer below 2^24, which float32 stores exactly.
		private const int PACK_MAX = 4095;
		private const int PACK_SHIFT = 4096;
		private const float PACK_SCALE = 1f / 16777216f;

		[SerializeField] private float m_borderWidth;
		[SerializeField] private float m_falloffDistance = 1;
		[SerializeField] private float m_falloffPower = 1;

		[SerializeField] private ModifierType m_modifierType = ModifierType.Uniform;
		[SerializeField] private float m_radius = 20;
		[SerializeField] private Vector4 m_freeRadius;
		[SerializeField] private ProceduralRectEdge m_edge;
		[SerializeField] private bool m_tiled;
		[SerializeField] private Vector2 m_tileFactor = new Vector2(1, 1);

		[SerializeField] private Vector2 m_edgeBulge;
		[SerializeField, Range(2, 64)] private int m_subdivisionsPerEdge = 16;

		[SerializeField, Tooltip("Ignore raycasts outside the rounded/bulged shape instead of using the full rect.")]
		private bool m_raycastUsesShape;

		private static Material s_defaultMaterial;

		private static Material DefaultMaterial {
			get {
				if (s_defaultMaterial == null) {
					s_defaultMaterial = new Material(Shader.Find("UI/Procedural Rect Image")) {
						name = "Procedural Rect Image (Default)",
						hideFlags = HideFlags.HideAndDontSave
					};
				}

				return s_defaultMaterial;
			}
		}

		public float BorderWidth {
			get => m_borderWidth;
			set {
				m_borderWidth = Mathf.Max(0, value);
				SetVerticesDirty();
			}
		}

		public float FalloffDistance {
			get => m_falloffDistance;
			set {
				m_falloffDistance = Mathf.Max(0, value);
				SetVerticesDirty();
			}
		}

		public float FalloffPower {
			get => m_falloffPower;
			set {
				m_falloffPower = Mathf.Clamp(value, MIN_FALLOFF_POWER, MAX_FALLOFF_POWER);
				SetVerticesDirty();
			}
		}

		public Vector2 EdgeBulge {
			get => m_edgeBulge;
			set {
				m_edgeBulge = value;
				SetVerticesDirty();
			}
		}

		public int SubdivisionsPerEdge {
			get => m_subdivisionsPerEdge;
			set {
				m_subdivisionsPerEdge = Mathf.Clamp(value, 2, 64);
				SetVerticesDirty();
			}
		}

		public bool RaycastUsesShape {
			get => m_raycastUsesShape;
			set => m_raycastUsesShape = value;
		}

		public override Material defaultMaterial => DefaultMaterial;

		private bool IsBulged => m_edgeBulge.sqrMagnitude > 0f;

		protected override void OnEnable()
		{
			base.OnEnable();
			FixTexCoordsInCanvas();
			preserveAspect = false;

			if (sprite == null) {
				sprite = EmptySprite.Get();
			}
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

		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			var rect = GetPixelAdjustedRect();
			var bulged = IsBulged;

			if (bulged) {
				GenerateBulgedQuad(toFill, rect);
			} else if (type == Type.Simple) {
				GenerateSimpleQuad(toFill, rect);
			} else {
				base.OnPopulateMesh(toFill);
			}

			EncodeAllInfoIntoVertices(toFill, rect, CalculateInfo(rect), bulged);
		}

		/// <summary>
		/// A quad covering the whole rect. Unlike Image's simple mesh this ignores sprite padding,
		/// since the shape is procedural and should always fill the rect.
		/// </summary>
		private void GenerateSimpleQuad(VertexHelper vh, Rect rect)
		{
			vh.Clear();

			var uv = GetOuterUv();
			var vertexColor = (Color32)color;

			vh.AddVert(new Vector3(rect.xMin, rect.yMin), vertexColor, new Vector4(uv.x, uv.y));
			vh.AddVert(new Vector3(rect.xMin, rect.yMax), vertexColor, new Vector4(uv.x, uv.w));
			vh.AddVert(new Vector3(rect.xMax, rect.yMax), vertexColor, new Vector4(uv.z, uv.w));
			vh.AddVert(new Vector3(rect.xMax, rect.yMin), vertexColor, new Vector4(uv.z, uv.y));

			vh.AddTriangle(0, 1, 2);
			vh.AddTriangle(2, 3, 0);
		}

		/// <summary>
		/// Generates fan triangulation where there is a center vertex and bulging vertices around the edges.
		/// uv0 holds the normalized (0-1) shape coordinate; EncodeAllInfoIntoVertices maps it to texture UVs.
		/// </summary>
		private void GenerateBulgedQuad(VertexHelper vh, Rect rect)
		{
			vh.Clear();

			var vertexColor = (Color32)color;
			var subdivisionsPerEdge = Mathf.Max(2, m_subdivisionsPerEdge);

			var uv0 = new Vector4(0.5f, 0.5f, 0, 0);
			Vector3 pos;

			// Center vertex
			vh.AddVert(new Vector3(rect.center.x, rect.center.y, 0f), vertexColor, uv0);

			// top edge: top-left -> top-right
			for (var i = 0; i < subdivisionsPerEdge; i++) {
				var edgeRatio = (float)i / subdivisionsPerEdge;
				var push = m_edgeBulge.y * Mathf.Sin(Mathf.PI * edgeRatio);

				uv0.x = edgeRatio;
				uv0.y = 1f;
				pos = new Vector3(rect.xMin + edgeRatio * rect.width, rect.yMax + push, 0f);
				vh.AddVert(pos, vertexColor, uv0);
			}

			// right edge: top-right -> bottom-right
			for (var i = 0; i < subdivisionsPerEdge; i++) {
				var edgeRatio = (float)i / subdivisionsPerEdge;
				var push = m_edgeBulge.x * Mathf.Sin(Mathf.PI * edgeRatio);

				uv0.x = 1f;
				uv0.y = 1f - edgeRatio;
				pos = new Vector3(rect.xMax + push, rect.yMax - edgeRatio * rect.height, 0f);
				vh.AddVert(pos, vertexColor, uv0);
			}

			// bottom edge: bottom-right -> bottom-left
			for (var i = 0; i < subdivisionsPerEdge; i++) {
				var edgeRatio = (float)i / subdivisionsPerEdge;
				var push = m_edgeBulge.y * Mathf.Sin(Mathf.PI * edgeRatio);

				uv0.x = 1f - edgeRatio;
				uv0.y = 0f;
				pos = new Vector3(rect.xMax - edgeRatio * rect.width, rect.yMin - push, 0f);
				vh.AddVert(pos, vertexColor, uv0);
			}

			// left edge: bottom-left -> top-left
			for (var i = 0; i < subdivisionsPerEdge; i++) {
				var edgeRatio = (float)i / subdivisionsPerEdge;
				var push = m_edgeBulge.x * Mathf.Sin(Mathf.PI * edgeRatio);

				uv0.x = 0f;
				uv0.y = edgeRatio;
				pos = new Vector3(rect.xMin - push, rect.yMin + edgeRatio * rect.height, 0f);
				vh.AddVert(pos, vertexColor, uv0);
			}

			// Fan triangulation from center
			var total = 4 * subdivisionsPerEdge;
			for (var i = 0; i < total; i++) {
				var next = (i + 1) % total;
				vh.AddTriangle(0, i + 1, next + 1);
			}
		}

		private ProceduralRectInfo CalculateInfo(Rect rect)
		{
			var falloff = Mathf.Max(0, m_falloffDistance);
			var aaScale = falloff > 0 ? 1f / falloff : MAX_AA_SCALE;

			// The mesh is expanded by half the falloff on every side, and the shader works in that expanded space.
			var expandedWidth = Mathf.Abs(rect.width) + falloff;
			var expandedHeight = Mathf.Abs(rect.height) + falloff;
			var expandedMinSide = Mathf.Min(expandedWidth, expandedHeight);

			// Grow the radii by the same amount so the expanded shape stays concentric with the original.
			var halfFalloff = falloff * 0.5f;
			var radius = FixRadius(CalculateRadius(rect), rect) + new Vector4(halfFalloff, halfFalloff, halfFalloff, halfFalloff);

			var normalizedRadius = expandedMinSide > 0 ? radius / expandedMinSide : Vector4.zero;
			var normalizedBorderWidth = expandedMinSide > 0 ? m_borderWidth * 2f / expandedMinSide : 0f;

			return new ProceduralRectInfo(
				expandedWidth,
				expandedHeight,
				falloff,
				aaScale,
				Mathf.Clamp(m_falloffPower, MIN_FALLOFF_POWER, MAX_FALLOFF_POWER),
				normalizedRadius,
				normalizedBorderWidth);
		}

		private Vector4 CalculateRadius(Rect imageRect)
		{
			switch (m_modifierType) {
				case ModifierType.Round:
					var r = Mathf.Min(Mathf.Abs(imageRect.width), Mathf.Abs(imageRect.height)) * 0.5f;
					return new Vector4(r, r, r, r);
				case ModifierType.Uniform:
					return new Vector4(m_radius, m_radius, m_radius, m_radius);
				case ModifierType.OneEdge:
					return m_edge switch {
						ProceduralRectEdge.Top => new Vector4(m_radius, m_radius, 0, 0),
						ProceduralRectEdge.Right => new Vector4(0, m_radius, m_radius, 0),
						ProceduralRectEdge.Bottom => new Vector4(0, 0, m_radius, m_radius),
						ProceduralRectEdge.Left => new Vector4(m_radius, 0, 0, m_radius),
						_ => Vector4.zero
					};
				case ModifierType.Free:
					return m_freeRadius;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Scales the radii (TL, TR, BR, BL) down so that adjacent corners never overlap.
		/// </summary>
		private static Vector4 FixRadius(Vector4 radius, Rect rect)
		{
			radius = Vector4.Max(radius, Vector4.zero);

			var width = Mathf.Abs(rect.width);
			var height = Mathf.Abs(rect.height);

			var scale = 1f;
			scale = LimitScale(scale, width, radius.x + radius.y);
			scale = LimitScale(scale, width, radius.z + radius.w);
			scale = LimitScale(scale, height, radius.x + radius.w);
			scale = LimitScale(scale, height, radius.y + radius.z);

			return radius * scale;
		}

		private static float LimitScale(float scale, float side, float radiusSum)
		{
			return radiusSum > side ? Mathf.Min(scale, side / radiusSum) : scale;
		}

		private void EncodeAllInfoIntoVertices(VertexHelper vh, Rect rect, ProceduralRectInfo info, bool uvIsNormalized)
		{
			var vert = new UIVertex();

			// Vertex channel layout:
			// uv0.xy = texture uv, uv0.zw = normalized (0-1) shape coordinate
			// uv1 = expanded rect width, height
			// uv2 = packed corner radii: (TL, TR) and (BR, BL)
			// uv3.x = packed shape mode/line weight and falloff power
			// uv3.y = edge falloff scale
			var uv1 = new Vector4(info.Width, info.Height, 0, 0);
			var uv2 = new Vector4(Pack(info.Radius.x, info.Radius.y), Pack(info.Radius.z, info.Radius.w), 0, 0);

			var shapeMode = info.BorderWidth <= 0 ? 1f : Mathf.Clamp01(info.BorderWidth);
			var normalizedFalloffPower = Mathf.InverseLerp(MIN_FALLOFF_POWER, MAX_FALLOFF_POWER, info.FalloffPower);
			var uv3 = new Vector4(Pack(shapeMode, normalizedFalloffPower), info.AAScale, 0, 0);

			var outerUv = GetOuterUv();
			var tiling = CalculateTiling(rect);
			var invWidth = rect.width != 0 ? 1f / rect.width : 0f;
			var invHeight = rect.height != 0 ? 1f / rect.height : 0f;
			var center = new Vector2(0.5f, 0.5f);

			for (var i = 0; i < vh.currentVertCount; i++) {
				vh.PopulateUIVertex(ref vert, i);

				// Derive the shape coordinate from position rather than uv0: uv0 holds atlas/sprite-sheet UVs,
				// which only span 0-1 for a sprite that uses its whole texture.
				Vector2 normalized;
				if (uvIsNormalized) {
					normalized = vert.uv0;
				} else {
					normalized = new Vector2((vert.position.x - rect.xMin) * invWidth, (vert.position.y - rect.yMin) * invHeight);
				}

				Vector2 textureUv;
				if (m_tiled) {
					textureUv = Vector2.Scale(normalized, tiling);
				} else if (uvIsNormalized) {
					textureUv = new Vector2(Mathf.LerpUnclamped(outerUv.x, outerUv.z, normalized.x), Mathf.LerpUnclamped(outerUv.y, outerUv.w, normalized.y));
				} else {
					textureUv = vert.uv0;
				}

				vert.position += (Vector3)((normalized - center) * info.FallOffDistance);
				vert.uv0 = new Vector4(textureUv.x, textureUv.y, normalized.x, normalized.y);
				vert.uv1 = uv1;
				vert.uv2 = uv2;
				vert.uv3 = uv3;

				vh.SetUIVertex(vert, i);
			}
		}

		private Vector4 GetOuterUv()
		{
			var active = overrideSprite;
			return active != null ? DataUtility.GetOuterUV(active) : new Vector4(0, 0, 1, 1);
		}

		/// <summary>
		/// Tile counts across the rect, using the same pixels-per-unit rules as Image. Tiling repeats 0-1 UVs,
		/// so it needs a non-atlased texture with Wrap Mode = Repeat.
		/// </summary>
		private Vector2 CalculateTiling(Rect rect)
		{
			if (!m_tiled) {
				return Vector2.one;
			}

			var active = overrideSprite;
			var texture = mainTexture;
			var spriteSize = active != null ? active.rect.size : texture != null ? new Vector2(texture.width, texture.height) : Vector2.one;
			var tileSize = spriteSize / Mathf.Max(0.0001f, pixelsPerUnit * pixelsPerUnitMultiplier);

			return new Vector2(
				tileSize.x > 0 ? Mathf.Abs(rect.width) / tileSize.x * m_tileFactor.x : 1f,
				tileSize.y > 0 ? Mathf.Abs(rect.height) / tileSize.y * m_tileFactor.y : 1f);
		}

		private static float Pack(float a, float b)
		{
			var high = Mathf.RoundToInt(Mathf.Clamp01(a) * PACK_MAX);
			var low = Mathf.RoundToInt(Mathf.Clamp01(b) * PACK_MAX);
			return (high * PACK_SHIFT + low) * PACK_SCALE;
		}

		public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
		{
			if (!base.IsRaycastLocationValid(screenPoint, eventCamera)) {
				return false;
			}

			if (!m_raycastUsesShape) {
				return true;
			}

			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var localPoint)) {
				return false;
			}

			return IsInsideShape(localPoint, GetPixelAdjustedRect());
		}

		/// <summary>
		/// Tests a local point against the visible shape (the original rect edge, ignoring falloff).
		/// Note: GraphicRaycaster rejects points outside the RectTransform before this runs, so use a
		/// negative Raycast Padding if bulged areas outside the rect should be clickable.
		/// </summary>
		private bool IsInsideShape(Vector2 point, Rect rect)
		{
			if (rect.width <= 0 || rect.height <= 0) {
				return false;
			}

			if (IsBulged) {
				point = RemoveBulge(point, rect);
			}

			if (!rect.Contains(point)) {
				return false;
			}

			var radius = FixRadius(CalculateRadius(rect), rect);

			var left = point.x - rect.xMin;
			var right = rect.xMax - point.x;
			var bottom = point.y - rect.yMin;
			var top = rect.yMax - point.y;

			return !IsOutsideCorner(left, top, radius.x)
			       && !IsOutsideCorner(right, top, radius.y)
			       && !IsOutsideCorner(right, bottom, radius.z)
			       && !IsOutsideCorner(left, bottom, radius.w);
		}

		private static bool IsOutsideCorner(float edgeDistanceX, float edgeDistanceY, float radius)
		{
			if (radius <= 0 || edgeDistanceX >= radius || edgeDistanceY >= radius) {
				return false;
			}

			var offset = new Vector2(radius - edgeDistanceX, radius - edgeDistanceY);
			return offset.sqrMagnitude > radius * radius;
		}

		/// <summary>
		/// Maps a point in the bulged shape back into the un-bulged rect (inverse of GenerateBulgedQuad's edge push).
		/// </summary>
		private Vector2 RemoveBulge(Vector2 point, Rect rect)
		{
			var center = rect.center;
			var halfSize = rect.size * 0.5f;

			var tx = Mathf.Clamp01((point.x - rect.xMin) / rect.width);
			var ty = Mathf.Clamp01((point.y - rect.yMin) / rect.height);

			var bulgedHalfWidth = halfSize.x + m_edgeBulge.x * Mathf.Sin(Mathf.PI * ty);
			var bulgedHalfHeight = halfSize.y + m_edgeBulge.y * Mathf.Sin(Mathf.PI * tx);

			if (bulgedHalfWidth <= 0 || bulgedHalfHeight <= 0) {
				return new Vector2(float.MaxValue, float.MaxValue);
			}

			return new Vector2(
				center.x + (point.x - center.x) / bulgedHalfWidth * halfSize.x,
				center.y + (point.y - center.y) / bulgedHalfHeight * halfSize.y);
		}

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			sprite = EmptySprite.Get();
			preserveAspect = false;
			FixTexCoordsInCanvas();
			SetAllDirty();
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			if (sprite == null) {
				sprite = EmptySprite.Get();
			}

			m_falloffDistance = Mathf.Max(0, m_falloffDistance);
			m_borderWidth = Mathf.Max(0, m_borderWidth);
			m_falloffPower = Mathf.Clamp(m_falloffPower, MIN_FALLOFF_POWER, MAX_FALLOFF_POWER);
			m_subdivisionsPerEdge = Mathf.Clamp(m_subdivisionsPerEdge, 2, 64);
		}
#endif
	}

	public struct ProceduralRectInfo
	{
		public float Width;
		public float Height;
		public float FallOffDistance;
		public Vector4 Radius;
		public float BorderWidth;
		public float AAScale;
		public float FalloffPower;

		public ProceduralRectInfo(float width, float height, float fallOffDistance, float aaScale, float falloffPower,
			Vector4 radius, float borderWidth)
		{
			Width = Mathf.Abs(width);
			Height = Mathf.Abs(height);
			FallOffDistance = Mathf.Max(0, fallOffDistance);
			Radius = radius;
			BorderWidth = Mathf.Max(borderWidth, 0);
			AAScale = Mathf.Max(0, aaScale);
			FalloffPower = Mathf.Clamp(falloffPower, ProceduralRectImage.MIN_FALLOFF_POWER, ProceduralRectImage.MAX_FALLOFF_POWER);
		}
	}
}
