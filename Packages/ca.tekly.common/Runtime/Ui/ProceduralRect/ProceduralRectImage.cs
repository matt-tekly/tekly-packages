using System;
using UnityEngine;
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
		
		[SerializeField] private float m_borderWidth;
		[SerializeField] private float m_falloffDistance = 1;
		[SerializeField] private float m_falloffPower = 1;
		
		[SerializeField] private ModifierType m_modifierType = ModifierType.Uniform;
		[SerializeField] private float m_radius = 20;
		[SerializeField] private Vector4 m_freeRadius;
		[SerializeField] private ProceduralRectEdge m_edge;
		
		private static Material s_materialInstance;

		private static Material DefaultMaterial {
			get {
				if (s_materialInstance == null) {
					s_materialInstance = new Material(Shader.Find("UI/Procedural Rect Image"));
				}

				return s_materialInstance;
			}
		}
		
		public float BorderWidth {
			get => m_borderWidth;
			set {
				m_borderWidth = value;
				SetVerticesDirty();
			}
		}

		public float FalloffDistance {
			get => m_falloffDistance;
			set {
				m_falloffDistance = value;
				SetVerticesDirty();
			}
		}

		public float FalloffPower {
			get => m_falloffPower;
			set {
				m_falloffPower = value;
				SetVerticesDirty();
			}
		}
		
		public override Material material {
			get => m_Material == null ? DefaultMaterial : base.material;
			set => base.material = value;
		}

		public override Material defaultMaterial => DefaultMaterial;


		protected override void OnEnable()
		{
			base.OnEnable();
			FixTexCoordsInCanvas();
			preserveAspect = false;
			
			sprite = EmptySprite.Get();
		}

		private void FixTexCoordsInCanvas()
		{
			if (canvas != null) {
				canvas.additionalShaderChannels |= NEEDED_SHADER_CHANNELS;
			}
		}

		/// <summary>
		/// Prevents radius to get bigger than rect size
		/// </summary>
		/// <returns>The fixed radius.</returns>
		/// <param name="vec">border-radius as Vector4 (starting upper-left, clockwise)</param>
		private Vector4 FixRadius(Vector4 vec)
		{
			var r = rectTransform.rect;
			vec = new Vector4(Mathf.Max(vec.x, 0), Mathf.Max(vec.y, 0), Mathf.Max(vec.z, 0), Mathf.Max(vec.w, 0));
			
			var scaleFactor = Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(r.width / (vec.x + vec.y), r.width / (vec.z + vec.w)), r.height / (vec.x + vec.w)), r.height / (vec.z + vec.y)), 1f);
			return vec * scaleFactor;
		}

		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			base.OnPopulateMesh(toFill);
			EncodeAllInfoIntoVertices(toFill, CalculateInfo());
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();
			FixTexCoordsInCanvas();
		}

		private ProceduralRectInfo CalculateInfo()
		{
			var r = GetPixelAdjustedRect();
			var aaScale = m_falloffDistance > 0 ? 1f / m_falloffDistance : 2048f;

			var radius = FixRadius(CalculateRadius(r));

			var minSide = Mathf.Min(r.width, r.height);
			var normalizedBorderWidth = minSide > 0 ? m_borderWidth / minSide * 2f : 0f;
			var normalizedRadius = minSide > 0 ? radius / minSide : Vector4.zero;

			return new ProceduralRectInfo(
				r.width + m_falloffDistance,
				r.height + m_falloffDistance,
				m_falloffDistance,
				aaScale,
				Mathf.Clamp(m_falloffPower, 0.25f, 4f),
				normalizedRadius,
				normalizedBorderWidth);
		}

		private Vector4 CalculateRadius(Rect imageRect)
		{
			switch (m_modifierType) {
				case ModifierType.Round:
					var r = Mathf.Min(imageRect.width, imageRect.height) * 0.5f;
					return new Vector4(r, r, r, r);
				case ModifierType.Uniform:
					return new Vector4(m_radius, m_radius, m_radius, m_radius);
				case ModifierType.OneEdge:
					switch (m_edge) {
						case ProceduralRectEdge.Top:
							return new Vector4(m_radius, m_radius, 0, 0);
						case ProceduralRectEdge.Right:
							return new Vector4(0, m_radius, m_radius, 0);
						case ProceduralRectEdge.Bottom:
							return new Vector4(0, 0, m_radius, m_radius);
						case ProceduralRectEdge.Left:
							return new Vector4(m_radius, 0, 0, m_radius);
						default:
							return new Vector4(0, 0, 0, 0);
					}
				case ModifierType.Free:
					return m_freeRadius;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void EncodeAllInfoIntoVertices(VertexHelper vh, ProceduralRectInfo info)
		{
			var vert = new UIVertex();

			// Vertex channel layout:
			// uv1 = rect width, rect height
			// uv2 = packed corner radii: (TL, TR) and (BR, BL)
			// uv3.x = packed shape mode/line weight and falloff power
			// uv3.y = edge falloff scale
			var uv1 = new Vector2(info.Width, info.Height);
			var uv2 = new Vector2(EncodeFloats_0_1_16_16(info.Radius.x, info.Radius.y), EncodeFloats_0_1_16_16(info.Radius.z, info.Radius.w));

			var shapeMode = info.BorderWidth == 0 ? 1f : Mathf.Clamp01(info.BorderWidth);
			var normalizedFalloffPower = Mathf.InverseLerp(0.25f, 4f, info.FalloffPower);
			var uv3 = new Vector2(EncodeFloats_0_1_16_16(shapeMode, normalizedFalloffPower), info.AAScale);

			for (var i = 0; i < vh.currentVertCount; i++) {
				vh.PopulateUIVertex(ref vert, i);

				vert.position += ((Vector3) vert.uv0 - new Vector3(0.5f, 0.5f)) * info.FallOffDistance;
				vert.uv1 = uv1;
				vert.uv2 = uv2;
				vert.uv3 = uv3;

				vh.SetUIVertex(vert, i);
			}
		}

		/// <summary>
		/// Encode two values between [0,1] into a single float. Each using 16 bits.
		/// </summary>
		private static float EncodeFloats_0_1_16_16(float a, float b)
		{
			var kDecodeDot = new Vector2(1.0f, 1f / 65535.0f);
			return Vector2.Dot(new Vector2(Mathf.Floor(a * 65534) / 65535f, Mathf.Floor(b * 65534) / 65535f),
				kDecodeDot);
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
			
			sprite = EmptySprite.Get();
			m_falloffDistance = Mathf.Max(0, m_falloffDistance);
			m_borderWidth = Mathf.Max(0, m_borderWidth);
			m_falloffPower = Mathf.Clamp(m_falloffPower, 0.25f, 4f);
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
			FalloffPower = Mathf.Clamp(falloffPower, 0.25f, 4f);
		}
	}
}
