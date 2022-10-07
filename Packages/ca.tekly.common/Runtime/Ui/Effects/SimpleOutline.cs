using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Tekly.Common.Ui.Effects
{
	[AddComponentMenu("UI/Effects/Simple outline", 81)]
	public class SimpleOutline : BaseMeshEffect
	{
		[SerializeField] private Color m_effectColor = new Color(0f, 0f, 0f, 0.5f);
		[SerializeField] private Vector2 m_effectDistance = new Vector2(1f, 1f);
		[SerializeField] private bool m_useGraphicAlpha = true;

		private const float MaxEffectDistance = 600f;

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			EffectDistance = m_effectDistance;
			base.OnValidate();
		}
#endif

		/// <summary>
		/// Color for the effect
		/// </summary>
		public Color EffectColor {
			get => m_effectColor;
			set {
				m_effectColor = value;
				if (graphic != null) {
					graphic.SetVerticesDirty();
				}
			}
		}

		/// <summary>
		/// How far is the shadow from the graphic.
		/// </summary>
		public Vector2 EffectDistance {
			get => m_effectDistance;
			set {
				if (value.x > MaxEffectDistance) {
					value.x = MaxEffectDistance;
				}

				if (value.x < -MaxEffectDistance) {
					value.x = -MaxEffectDistance;
				}

				if (value.y > MaxEffectDistance) {
					value.y = MaxEffectDistance;
				}

				if (value.y < -MaxEffectDistance) {
					value.y = -MaxEffectDistance;
				}

				if (m_effectDistance == value) {
					return;
				}

				m_effectDistance = value;

				if (graphic != null) {
					graphic.SetVerticesDirty();
				}
			}
		}

		/// <summary>
		/// Should the shadow inherit the alpha from the graphic?
		/// </summary>
		public bool UseGraphicAlpha {
			get => m_useGraphicAlpha;
			set {
				m_useGraphicAlpha = value;
				if (graphic != null) {
					graphic.SetVerticesDirty();
				}
			}
		}

		public override void ModifyMesh(VertexHelper vh)
		{
			if (!IsActive()) {
				return;
			}

			var output = ListPool<UIVertex>.Get();
			vh.GetUIVertexStream(output);

			ApplyOutline(output, EffectColor, 0, output.Count, EffectDistance.x, EffectDistance.y);
			vh.Clear();
			vh.AddUIVertexTriangleStream(output);
			ListPool<UIVertex>.Release(output);
		}

		private void ApplyOutline(List<UIVertex> verts, Color32 color, int start, int end, float x, float y)
		{
			var neededCapacity = verts.Count + end - start;
			if (verts.Capacity < neededCapacity) {
				verts.Capacity = neededCapacity;
			}
			
			var center = Vector3.zero;

			for (var i = start; i < end; ++i) {
				center += verts[i].position;
			}

			center /= end - start;

			for (var i = start; i < end; ++i) {
				var vt = verts[i];
				verts.Add(vt);

				var v = vt.position;
				
				if (v.x < center.x) {
					v.x -= x;
				} else {
					v.x += x;
				}

				if (v.y < center.y) {
					v.y -= y;
				} else {
					v.y += y;
				}

				vt.position = v;
				
				var newColor = color;
				
				if (m_useGraphicAlpha) {
					newColor.a = (byte) (newColor.a * vt.color.a / 255);
				}
					
				vt.color = newColor;
				verts[i] = vt;
			}
		}
	}
}