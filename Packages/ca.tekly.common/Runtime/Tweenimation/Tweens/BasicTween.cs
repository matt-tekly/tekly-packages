using System;
using UnityEngine;

namespace Tekly.Common.Tweenimation.Tweens
{
    [Serializable]
    public abstract class BasicTween : BaseTween
    {
        [SerializeField] protected float m_duration = 1;
        [SerializeField] protected EaseData m_ease;

        public override float TotalTime => m_delay + m_duration;
        
        public sealed override void Evaluate(float time)
        {
            if (!m_enabled || time <= m_delay) {
                return;
            }

            if (time <= TotalTime) {
                OnEvaluate(m_ease.Evaluate((time - m_delay) / m_duration));    
            } else {
                OnEvaluate(m_ease.Evaluate(1));
            }
        }
    }
}