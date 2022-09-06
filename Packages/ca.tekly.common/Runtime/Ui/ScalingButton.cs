using UnityEngine;

namespace Tekly.Common.Ui
{
    public class ScalingButton : SimpleTweenButton
    {
        [SerializeField] RectTransform m_scaleRoot;
        [SerializeField] private float m_scale = 0.95f;
        
        [SerializeField] private CanvasRenderer m_canvasRenderer;
        
        protected override void SetPressedRatio(float ratio)
        {
            if (m_scaleRoot != null) {
                var scale = Mathf.Lerp(1, m_scale, ratio);
                m_scaleRoot.localScale = new Vector3(scale, scale, scale);    
            }
        }

        protected override void SetRendererColor(Color color)
        {
            if (m_canvasRenderer != null) {
                m_canvasRenderer.SetColor(color);
            }
        }
    }
}