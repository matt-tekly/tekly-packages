using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Tekly.Common.Ui.Effects
{
    public class ImageTiler : BaseMeshEffect
    {
        [SerializeField] private float m_horizontalScale = 1f;
        [SerializeField] private float m_verticalScale = 1f;
		
        [NonSerialized] private Image m_image;
        [NonSerialized] private RectTransform m_rectTransform;

        private Image Image {
            get {
                if (m_image == null) {
                    m_image = GetComponent<Image>();
                }

                return m_image;
            }
        }
		
        private RectTransform RectTransform {
            get {
                if (m_rectTransform == null) {
                    m_rectTransform = GetComponent<RectTransform>();
                }

                return m_rectTransform;
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!enabled) {
                return;
            }
			
            List<UIVertex> buffer = ListPool<UIVertex>.Get();
            vh.GetUIVertexStream(buffer);
            Apply(buffer);
            vh.AddUIVertexTriangleStream(buffer);
            ListPool<UIVertex>.Release(buffer);
        }

        private void Apply(List<UIVertex> verts)
        {
            var rect = RectTransform.rect;
            var spriteRect = Image.sprite.rect;
			
            var x = rect.width / spriteRect.width * m_horizontalScale;
            var y = rect.height / spriteRect.height * m_verticalScale;

            for (var i = 0; i < verts.Count; ++i) {
                var v = verts[i];

                v.uv0.x *= x;
                v.uv0.y *= y;

                verts[i] = v;
            }
        }
    }
}