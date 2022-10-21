using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Tekly.Common.Ui.Effects
{
    public class ImageFlip : BaseMeshEffect
    {
        [SerializeField] private bool m_horizontal = false;
        [SerializeField] private bool m_vertical = false;

        [NonSerialized] private RectTransform m_rectTransform;

        public bool Horizontal {
            get => m_horizontal;
            set {
                m_horizontal = value;
                graphic.SetVerticesDirty();
            }
        }

        public bool Vertical {
            get => m_vertical;
            set {
                m_vertical = value;
                graphic.SetVerticesDirty();
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
            List<UIVertex> buffer = ListPool<UIVertex>.Get();
            vh.GetUIVertexStream(buffer);
            ApplyFlip(buffer);
            vh.AddUIVertexTriangleStream(buffer);
            ListPool<UIVertex>.Release(buffer);
        }

        private void ApplyFlip(List<UIVertex> verts)
        {
            var center = RectTransform.rect.center;

            for (var i = 0; i < verts.Count; ++i) {
                var v = verts[i];
                
                v.position = new Vector3(
                    (m_horizontal ? (v.position.x + (center.x - v.position.x) * 2) : v.position.x),
                    (m_vertical ? (v.position.y + (center.y - v.position.y) * 2) : v.position.y),
                    v.position.z
                );
                
                verts[i] = v;
            }
        }
    }
}