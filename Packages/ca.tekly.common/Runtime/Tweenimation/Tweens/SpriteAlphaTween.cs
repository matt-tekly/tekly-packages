using System;
using Tekly.Common.Maths;
using UnityEngine;

namespace Tekly.Common.Tweenimation.Tweens
{
    [Serializable]
    public class SpriteAlphaTween : BasicTween
    {
        [SerializeField] private FloatRange m_alpha;
        [SerializeField] private SpriteRenderer m_renderer;

        protected override void OnEvaluate(float ratio)
        {
            var color = m_renderer.color;
            color.a = Mathf.Lerp(m_alpha.Min, m_alpha.Max, ratio);
            m_renderer.color = color;
        }
    }
}