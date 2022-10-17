using System;
using UnityEngine;

namespace Tekly.Common.Tweenimation.Tweens
{
    [Serializable]
    public class ScaleTween : BasicTween
    {
        [SerializeField] private Transform m_transform;
        [SerializeField] private OriginMode m_originMode;
        
        [SerializeField] private Vector3 m_start = Vector3.one;
        [SerializeField] private Vector3 m_end = Vector3.one * 1.5f;
        
        private Vector3 m_original;

        protected override void OnInitialize()
        {
            m_original = m_transform.localScale;
        }

        protected override void OnPlay(bool reinitialize)
        {
            if (reinitialize) {
                m_original = m_transform.localScale;
            }
        }
        
        protected override void OnEvaluate(float ratio)
        {
            Vector3 start;
            Vector3 end;
            
            switch (m_originMode) {
                case OriginMode.Relative:
                    start = m_original + m_start;
                    end = m_original + m_end;
                    break;
                case OriginMode.Fixed:
                    start = m_start;
                    end = m_end;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            m_transform.localScale = Vector3.LerpUnclamped(start, end, ratio);
        }
    }
}