using Tekly.Common.Ui.Styled;
using UnityEngine;
using UnityEngine.UI;
using GpuBlendMode = UnityEngine.Rendering.BlendMode;

namespace Tekly.Common.Ui
{
	/// <summary>How a vignette combines with what is behind it.</summary>
	public enum VignetteBlendMode
	{
		/// <summary>Drawn over what is behind, like any UI graphic.</summary>
		Normal = 0,

		/// <summary>
		/// Multiplies what is behind by the color, the way post processing vignettes do. A colored vignette
		/// tints what is behind and keeps its detail, where Normal would cover it with flat color.
		/// </summary>
		Multiply = 1
	}

	/// <summary>
	/// Darkens a rect toward its corners with the graphic's color. The color's alpha is how strong the vignette
	/// is, so tinting or fading the graphic animates it. The shape is a superellipse fitted to the rect, as post
	/// processing vignettes use: from almost a rounded rect to an ellipse, every curve of the fade the same shape
	/// at a smaller size, so it never creases.
	/// Every value goes in the vertices, so vignettes with the same blend mode share one material and batch.
	/// The middle, where the vignette is clear, is cut out of the mesh and only a ring around it is drawn, so
	/// a full screen vignette costs little more than its fade.
	/// </summary>
	[AddComponentMenu("UI/Vignette")]
	[RequireComponent(typeof(CanvasRenderer))] // Like Image: Graphic only adds one lazily, too late for a fresh component.
	public class Vignette : MaskableGraphic
	{
		/// <summary>How far in the vignette reaches: 0 only the very corners, 1 all the way to the center.</summary>
		public float Fade {
			get => m_fade;
			set => SetValue(ref m_fade, Mathf.Clamp01(value));
		}

		/// <summary>The shape: 0 is almost a rounded rect following the sides, 1 an ellipse touching the middle of each side.</summary>
		public float Roundness {
			get => m_roundness;
			set => SetValue(ref m_roundness, Mathf.Clamp01(value));
		}

		/// <summary>
		/// Bends the fade: pushes its halfway point toward the corners (negative) or toward the middle (positive),
		/// the same as a StyledPaint's Bias.
		/// </summary>
		public float Bias {
			get => m_bias;
			set => SetValue(ref m_bias, Mathf.Clamp(value, -1f, 1f));
		}

		/// <summary>How the vignette combines with what is behind it. Ignored when a custom Material is assigned.</summary>
		public VignetteBlendMode BlendMode {
			get => m_blendMode;
			set {
				if (m_blendMode == value) {
					return;
				}

				m_blendMode = value;
				SetMaterialDirty();
			}
		}

		public override Material defaultMaterial => GetMaterial(m_blendMode);

		[Tooltip("How far in the vignette reaches: 0 only the very corners, 1 all the way to the center.")]
		[SerializeField, Range(0, 1)] private float m_fade = 0.6f;

		[Tooltip("The shape: 0 is almost a rounded rect following the sides, 1 an ellipse touching the middle of each side.")]
		[SerializeField, Range(0, 1)] private float m_roundness = 0.5f;

		[Tooltip("Bends the fade: pushes its halfway point toward the corners (negative) or toward the middle (positive).")]
		[SerializeField, Range(-1, 1)] private float m_bias;

		[Tooltip("How the vignette combines with what is behind it. Multiply tints what is behind, the way post processing vignettes do. Ignored when a custom Material is assigned.")]
		[SerializeField] private VignetteBlendMode m_blendMode;

		private const string SHADER_RESOURCE = "Shaders/Vignette";
		private const AdditionalCanvasShaderChannels NEEDED_SHADER_CHANNELS = AdditionalCanvasShaderChannels.TexCoord1;

		// Below this half size of the clear middle, the ring's extra triangles cost more than the pixels saved.
		private const float MIN_CLEAR_HALF_SIZE = 16f;

		// The superellipse exponent at a roundness of 0 and 1. 2 is an ellipse, higher is squarer.
		private const float SQUARE_EXPONENT = 12f;
		private const float ROUND_EXPONENT = 2f;

		private static readonly Material[] s_materials = new Material[2];
		private static readonly int s_srcBlendId = Shader.PropertyToID("_SrcBlend");
		private static readonly int s_dstBlendId = Shader.PropertyToID("_DstBlend");
		private static readonly int s_multiplyId = Shader.PropertyToID("_Multiply");

		protected override void OnEnable()
		{
			base.OnEnable();
			EnsureShaderChannels();
		}

		protected override void OnCanvasHierarchyChanged()
		{
			base.OnCanvasHierarchyChanged();
			EnsureShaderChannels();
		}

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			color = new Color(0, 0, 0, 0.75f);
			raycastTarget = false;
		}

		protected override void OnValidate()
		{
			m_fade = Mathf.Clamp01(m_fade);
			m_roundness = Mathf.Clamp01(m_roundness);
			m_bias = Mathf.Clamp(m_bias, -1f, 1f);
			base.OnValidate();
		}
#endif

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			var rect = GetPixelAdjustedRect();
			var center = rect.center;
			var halfSize = rect.size * 0.5f;

			if (halfSize.x <= 0 || halfSize.y <= 0) {
				return;
			}

			// The fade runs from its start radius out to the corners' radius, so the corners get the full color.
			var exponent = Mathf.Lerp(SQUARE_EXPONENT, ROUND_EXPONENT, Mathf.Clamp01(m_roundness));
			var cornerRadius = Mathf.Pow(2f, 1f / exponent);
			var startRadius = (1f - Mathf.Clamp01(m_fade)) * cornerRadius;
			// The bias bends the fade exactly as it bends a StyledPaint's gradient.
			var biasExponent = new StyledPaint { Bias = m_bias }.BiasExponent;
			var shape = new Vector4(exponent, startRadius, cornerRadius, biasExponent);
			var vertexColor = (Color32)color;

			// Outer corners, then the clear middle's corners, each bottom-left and clockwise.
			AddCorners(vh, center, halfSize, halfSize, vertexColor, shape);

			// The largest rect in the rect's own proportions inside the clear superellipse: its corners sit on
			// the curve where it crosses the diagonal.
			var clear = halfSize * (startRadius / cornerRadius);

			if (clear.x < MIN_CLEAR_HALF_SIZE || clear.y < MIN_CLEAR_HALF_SIZE) {
				vh.AddTriangle(0, 1, 2);
				vh.AddTriangle(2, 3, 0);
				return;
			}

			AddCorners(vh, center, clear, halfSize, vertexColor, shape);

			// One trapezoid per side, between an outer edge and the clear middle's matching edge.
			for (var i = 0; i < 4; i++) {
				var next = (i + 1) % 4;
				vh.AddTriangle(i, next, 4 + next);
				vh.AddTriangle(i, 4 + next, 4 + i);
			}
		}

		private static void AddCorners(VertexHelper vh, Vector2 center, Vector2 corner, Vector2 halfSize,
			Color32 vertexColor, Vector4 shape)
		{
			AddVertex(vh, center, new Vector2(-corner.x, -corner.y), halfSize, vertexColor, shape);
			AddVertex(vh, center, new Vector2(-corner.x, corner.y), halfSize, vertexColor, shape);
			AddVertex(vh, center, new Vector2(corner.x, corner.y), halfSize, vertexColor, shape);
			AddVertex(vh, center, new Vector2(corner.x, -corner.y), halfSize, vertexColor, shape);
		}

		/// <summary>uv0 is the offset from the center, 1 at the sides. uv1 is the shape.</summary>
		private static void AddVertex(VertexHelper vh, Vector2 center, Vector2 offset, Vector2 halfSize,
			Color32 vertexColor, Vector4 shape)
		{
			var uv0 = new Vector4(offset.x / halfSize.x, offset.y / halfSize.y, 0, 0);
			vh.AddVert(center + offset, vertexColor, uv0, shape, Vector3.back, new Vector4(1, 0, 0, -1));
		}

		/// <summary>One shared material per blend mode, so vignettes with the same mode batch together.</summary>
		private static Material GetMaterial(VignetteBlendMode blendMode)
		{
			var index = Mathf.Clamp((int)blendMode, 0, s_materials.Length - 1);
			var material = s_materials[index];

			if (material != null) {
				return material;
			}

			var multiply = index == (int)VignetteBlendMode.Multiply;

			material = new Material(Resources.Load<Shader>(SHADER_RESOURCE)) {
				name = $"Vignette ({(VignetteBlendMode)index})",
				hideFlags = HideFlags.HideAndDontSave
			};

			// Multiply: behind * color. Normal: premultiplied alpha.
			material.SetFloat(s_srcBlendId, (float)(multiply ? GpuBlendMode.DstColor : GpuBlendMode.One));
			material.SetFloat(s_dstBlendId, (float)(multiply ? GpuBlendMode.Zero : GpuBlendMode.OneMinusSrcAlpha));
			material.SetFloat(s_multiplyId, multiply ? 1f : 0f);

			s_materials[index] = material;
			return material;
		}

		private void SetValue(ref float field, float value)
		{
			if (field == value) {
				return;
			}

			field = value;
			SetVerticesDirty();
		}

		/// <summary>The shape goes in uv1, which a canvas only passes to the shader when asked to.</summary>
		private void EnsureShaderChannels()
		{
			var target = canvas;

			if (target == null) {
				return;
			}

			EnsureShaderChannels(target);
			EnsureShaderChannels(target.rootCanvas);
		}

		private static void EnsureShaderChannels(Canvas target)
		{
			if (target != null && (target.additionalShaderChannels & NEEDED_SHADER_CHANNELS) != NEEDED_SHADER_CHANNELS) {
				target.additionalShaderChannels |= NEEDED_SHADER_CHANNELS;
			}
		}
	}
}
