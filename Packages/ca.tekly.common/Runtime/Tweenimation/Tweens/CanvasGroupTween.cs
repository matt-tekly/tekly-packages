using System;
using Tekly.Common.Maths;
using UnityEngine;

namespace Tekly.Common.Tweenimation.Tweens
{
    [Serializable]
    public class CanvasGroupTween : BasicTween
    {
        [SerializeField] private FloatRange m_alpha;
        [SerializeField] private CanvasGroup m_canvasGroup;
        
        protected override void OnEvaluate(float ratio)
        {
            m_canvasGroup.alpha = Mathf.Lerp(m_alpha.Min, m_alpha.Max, ratio);
        }
    }
}