using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui.Effects
{
	public enum GradientType
	{
		TwoColors,
		Corners,
		Gradient
	}

	[AddComponentMenu("UI/Effects/Gradient")]
	public class GradientEffect : BaseMeshEffect
	{
		[SerializeField] private GradientType m_gradientType;

		[SerializeField] private Color m_colorUpperLeft = Color.white;
		[SerializeField] private Color m_colorUpperRight = Color.white;
		[SerializeField] private Color m_colorLowerRight = Color.gray;
		[SerializeField] private Color m_colorLowerLeft = Color.gray;

		[SerializeField] private Gradient m_gradient;

		[SerializeField] [Range(0, 1)] private float m_intensity = 1f;
		[SerializeField] [Range(0, 360)] private float m_angle;

		private RectTransform m_rectTransform;

		private const float MIN_KEY_TIME = 0.001f;
		private const float MAX_KEY_TIME = 0.999f;
		private static HashSet<float> s_cuts = new HashSet<float>();

		private RectTransform RectTransform {
			get {
				if (m_rectTransform == null) {
					m_rectTransform = transform as RectTransform;
				}

				return m_rectTransform;
			}
		}

		public GradientType GradientType {
			get => m_gradientType;
			set {
				if (m_gradientType != value) {
					m_gradientType = value;
					SetDirty();
				}
			}
		}

		public Gradient Gradient {
			get => m_gradient;
			set {
				m_gradient = value;
				SetDirty();
			}
		}

		public Color UpperLeft {
			get => m_colorUpperLeft;
			set {
				if (m_colorUpperLeft != value) {
					m_colorUpperLeft = value;
					SetDirty();
				}
			}
		}

		public Color UpperRight {
			get => m_colorUpperRight;
			set {
				if (m_colorUpperRight != value) {
					m_colorUpperRight = value;
					SetDirty();
				}
			}
		}

		public Color LowerRight {
			get => m_colorLowerRight;
			set {
				if (m_colorLowerRight != value) {
					m_colorLowerRight = value;
					SetDirty();
				}
			}
		}

		public Color LowerLeft {
			get => m_colorLowerLeft;
			set {
				if (m_colorLowerLeft != value) {
					m_colorLowerLeft = value;
					SetDirty();
				}
			}
		}

		public float Intensity {
			get => m_intensity;
			set {
				var clampedValue = Mathf.Clamp01(value);
				if (!Mathf.Approximately(m_intensity, clampedValue)) {
					m_intensity = clampedValue;
					SetDirty();
				}
			}
		}

		public float Angle {
			get => m_angle;
			set {
				var clampedAngle = value < 0 ? (value % 360) + 360 : value % 360;
				if (!Mathf.Approximately(m_angle, clampedAngle)) {
					m_angle = clampedAngle;
					SetDirty();
				}
			}
		}

		public override void ModifyMesh(VertexHelper vh)
		{
			if (!enabled) {
				return;
			}

			var rect = RectTransform.rect;

			if (m_gradientType == GradientType.Gradient) {
				s_cuts.Clear();

				foreach (var key in m_gradient.alphaKeys) {
					if (key.time is < MIN_KEY_TIME or > MAX_KEY_TIME) {
						continue;
					}

					s_cuts.Add(key.time);
				}

				foreach (var key in m_gradient.colorKeys) {
					if (key.time is < MIN_KEY_TIME or > MAX_KEY_TIME) {
						continue;
					}

					s_cuts.Add(key.time);
				}

				GradientMeshUtils.CutMesh(vh, m_angle, rect, s_cuts);
			}

			var size = rect.max - rect.min;
			var min = rect.min;

			var vert = new UIVertex();

			for (var i = 0; i < vh.currentVertCount; i++) {
				vh.PopulateUIVertex(ref vert, i);

				var normalizedPosition = ((Vector2) vert.position - min) / size;
				normalizedPosition = RotateNormalizedPosition(normalizedPosition, m_angle);

				Color gradientColor;

				if (m_gradientType == GradientType.Gradient) {
					gradientColor = m_gradient.Evaluate(normalizedPosition.y);
				} else {
					gradientColor = EvaluateCorners(m_colorUpperLeft, m_colorUpperRight,
						m_colorLowerRight, m_colorLowerLeft, normalizedPosition);
				}

				vert.color = Color.Lerp(vert.color, gradientColor * vert.color, m_intensity);

				vh.SetUIVertex(vert, i);
			}
		}

		private void SetDirty()
		{
			if (graphic != null) {
				graphic.SetVerticesDirty();
			}
		}

		private static Vector2 RotateNormalizedPosition(Vector2 normalizedPosition, float rotation)
		{
			var radians = Mathf.Deg2Rad * (rotation % 90);
			var scale = Mathf.Sin(radians) + Mathf.Cos(radians);

			var half = new Vector2(0.5f, 0.5f);

			// Move to center, rotate, move back
			return (normalizedPosition - half).Rotate(rotation) / scale + half;
		}

		private static Color EvaluateCorners(Color ul, Color ur, Color lr, Color ll, Vector2 uv)
		{
			return Color.Lerp(Color.Lerp(ll, lr, uv.x), Color.Lerp(ul, ur, uv.x), uv.y);
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			Angle = m_angle;
			base.OnValidate();
		}

		protected override void Reset()
		{
			base.Reset();

			m_gradientType = GradientType.TwoColors;
			m_colorUpperLeft = Color.white;
			m_colorUpperRight = Color.white;
			m_colorLowerRight = Color.gray;
			m_colorLowerLeft = Color.gray;
			
			m_gradient = new Gradient();

			var colorKey = new GradientColorKey[2];
			colorKey[0].color = Color.white;
			colorKey[0].time = 0f;
			colorKey[1].color = Color.gray;
			colorKey[1].time = 1f;

			var alphaKey = new GradientAlphaKey[2];
			alphaKey[0].alpha = 1f;
			alphaKey[0].time = 0f;
			alphaKey[1].alpha = 1f;
			alphaKey[1].time = 1f;

			m_gradient.SetKeys(colorKey, alphaKey);

			m_angle = 0;
			m_intensity = 1;
			
			SetDirty();
		}
#endif
	}
}