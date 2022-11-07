using Tekly.Common.Utils.Tweening;
using UnityEngine;

namespace Tekly.Common.Ui.ProgressBars
{
    public class FillAnimator : Filled
    {
        [SerializeReference] private Filled m_filled;
        [SerializeField] private float m_animationTime = 0.2f;
        
        [Range(0, 1)] [SerializeField] private float m_fill;
        [SerializeField] private EaseData m_ease;

        private float m_startTime;
        private float m_animStartValue;
        private float m_destinationValue;

        private bool m_animating;
        private bool m_hasReceivedValue;

        public override float Fill {
            get => m_fill;
            set {
                if (m_animationTime <= 0) {
                    Set(value);
                    return;
                }

                m_startTime = Time.time;

                if (m_hasReceivedValue) {
                    m_animStartValue = m_fill;
                    m_destinationValue = value;
                } else {
                    m_fill = value;
                    m_animStartValue = m_fill;
                    m_destinationValue = m_fill;
                    m_hasReceivedValue = true;
                }

                Set(m_fill);

                m_animating = true;
            }
        }

        public void SetWithoutAnimating(float value)
        {
            m_animating = false;
            m_fill = value;
            Set(m_fill);
        }
        
        private void Update()
        {
            if (!m_animating) {
                return;
            }

            var ratio = Mathf.InverseLerp(m_startTime, m_startTime + m_animationTime, Time.time);
            var animatedRatio = m_ease.Evaluate(ratio);

            m_fill = Mathf.Lerp(m_animStartValue, m_destinationValue, animatedRatio);
            Set(m_fill);

            if (Time.time > m_startTime + m_animationTime) {
                m_animating = false;
            }
        }

        private void Set(float value)
        {
            if (m_filled != null) {
                m_filled.Fill = value;    
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            Set(m_fill);
        }
#endif
    }
}