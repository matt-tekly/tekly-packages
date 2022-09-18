using System;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class ProgressBarBinder : Binder
    {
        [FormerlySerializedAs("Key")] [SerializeField] private ModelRef m_key;
        [FormerlySerializedAs("Rect")] [SerializeField] private RectTransform m_rect;

        [FormerlySerializedAs("AnimationTime")] [SerializeField] private float m_animationTime = 0.2f;

        [FormerlySerializedAs("StartValue")] [SerializeField] private float m_startValue;
        [FormerlySerializedAs("EndValue")] [SerializeField] private float m_endValue = 1;

        [FormerlySerializedAs("Animation")] [SerializeField] private AnimationCurve m_animation;

        private IDisposable m_disposable;

        private float m_startTime;

        private float m_animStartValue;
        private float m_currentValue;
        private float m_destinationValue;

        private bool m_animating;
        private bool m_hasReceivedValue;

        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out NumberValueModel numberValue)) {
                m_disposable?.Dispose();
                m_disposable = numberValue.Subscribe(BindValue);
            }
        }

        private void Update()
        {
            if (!m_animating) {
                return;
            }

            var ratio = Mathf.InverseLerp(m_startTime, m_startTime + m_animationTime, Time.time);
            var animatedRatio = m_animation.Evaluate(ratio);

            m_currentValue = Mathf.Lerp(m_animStartValue, m_destinationValue, animatedRatio);
            Set(m_currentValue);

            if (Time.time > m_startTime + m_animationTime) {
                m_animating = false;
            }
        }

        private void BindValue(double value)
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

        private void Set(float value)
        {
            m_rect.anchorMax = new Vector2(Mathf.Lerp(m_startValue, m_endValue, value), 1);
        }

        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }

        private void Reset()
        {
            m_animation = AnimationCurve.Linear(0, 0, 0.25f, 1);
        }
    }
}