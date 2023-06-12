using Tekly.Common.Utils.Tweening;
using UnityEngine;

namespace Tekly.Common.Ui.ProgressBars
{
    public class FillAnimator : Filled
    {
        [SerializeReference] private Filled m_filled;
        [SerializeField] private float m_animationTime = 0.2f;
        
        [SerializeField] private EaseData m_ease;

        private float m_currentFill;
        
        private float m_startTime;
        private float m_animStartValue;
        private float m_destinationValue;

        private bool m_animating;
        private bool m_hasReceivedValue;

        protected override void SetFill(float fill)
        {
            if (m_animationTime <= 0) {
                Set(fill);
                return;
            }

            m_startTime = Time.time;

            if (m_hasReceivedValue) {
                m_animStartValue = m_currentFill;
                m_destinationValue = fill;
            } else {
                m_currentFill = fill;
                m_animStartValue = m_currentFill;
                m_destinationValue = m_currentFill;
                m_hasReceivedValue = true;
            }

            Set(m_currentFill);

            m_animating = true;
        }

        public void SetWithoutAnimating(float value)
        {
            m_animating = false;
            m_currentFill = value;
            Set(m_currentFill);
        }
        
        private void Update()
        {
            if (!m_animating) {
                return;
            }

            var ratio = Mathf.InverseLerp(m_startTime, m_startTime + m_animationTime, Time.time);
            var animatedRatio = m_ease.Evaluate(ratio);

            m_currentFill = Mathf.Lerp(m_animStartValue, m_destinationValue, animatedRatio);
            Set(m_currentFill);

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
    }
}