using System;
using Tekly.Common.Utils.Tweening;
using UnityEngine;

namespace Tekly.Common.Ui.ProgressBars
{
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField] private bool m_vertical;
        [SerializeField] private RectTransform m_rect;

        [SerializeField] private float m_animationTime = 0.2f;

        [SerializeField] private float m_startValue;
        [SerializeField] private float m_endValue = 1;

        [SerializeField] private Ease m_ease = Ease.Linear;

        private IDisposable m_disposable;

        private float m_startTime;

        private float m_animStartValue;
        private float m_currentValue;
        private float m_destinationValue;

        private bool m_animating;
        private bool m_hasReceivedValue;

        public void SetProgress(double value)
        {
            if (m_animationTime <= 0) {
                Set((float) value);
                return;
            }

            m_startTime = Time.time;

            if (m_hasReceivedValue) {
                m_animStartValue = m_currentValue;
                m_destinationValue = (float) value;
            } else {
                m_currentValue = (float) value;
                m_animStartValue = m_currentValue;
                m_destinationValue = m_currentValue;
                m_hasReceivedValue = true;
            }

            Set(m_currentValue);

            m_animating = true;
        }

        private void Update()
        {
            if (!m_animating) {
                return;
            }

            var ratio = Mathf.InverseLerp(m_startTime, m_startTime + m_animationTime, Time.time);
            var animatedRatio = Easing.Evaluate(ratio, m_ease);

            m_currentValue = Mathf.Lerp(m_animStartValue, m_destinationValue, animatedRatio);
            Set(m_currentValue);

            if (Time.time > m_startTime + m_animationTime) {
                m_animating = false;
            }
        }

        private void Set(float value)
        {
            if (m_vertical) {
                m_rect.anchorMax = new Vector2(1, Mathf.Lerp(m_startValue, m_endValue, value));
            } else {
                m_rect.anchorMax = new Vector2(Mathf.Lerp(m_startValue, m_endValue, value), 1);
            }
        }

        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
    }
}