using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui
{
    public class PushButton : SimpleTweenButton
    {
        public RectTransform VisualRoot;
        public CanvasRenderer CanvasRenderer;
        public Shadow Shadow;
		
        public float PressedOffset = -10;
		
        private Vector2 m_originalShadowDistance;
		
        protected override void Awake()
        {
            if (Shadow != null) {
                m_originalShadowDistance = Shadow.effectDistance;
            }
        }
		
        protected override void SetPressedRatio(float ratio)
        {
            var offset = Mathf.Lerp(0, PressedOffset, ratio);
            VisualRoot.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, -offset, VisualRoot.rect.height);
            if (Shadow != null) {
                Shadow.effectDistance = Vector2.Lerp(m_originalShadowDistance, Vector2.zero, ratio);
            }
        }
		
        protected override void SetRendererColor(Color color)
        {
            if (CanvasRenderer != null) {
                CanvasRenderer.SetColor(color);
            }
        }
    }
}