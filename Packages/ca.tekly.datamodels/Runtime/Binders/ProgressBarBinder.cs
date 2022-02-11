using System;
using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public class ProgressBarBinder : Binder
    {
        public ModelRef Key;
        public RectTransform Rect;
        
        public float AnimationTime = 0.2f;
        
        public float StartValue;
        public float EndValue = 1;
        
        public AnimationCurve Animation;
        
        private IDisposable m_disposable;

        private float m_startTime;
        
        private float m_startValue;
        private float m_currentValue;
        private float m_destinationValue;

        private bool m_animating;
        private bool m_hasReceivedValue;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(Key.Path, out NumberValueModel numberValue)) {
                m_disposable?.Dispose();
                m_disposable = numberValue.Subscribe(BindValue);
            }
        }

        private void Update()
        {
            if (!m_animating) {
                return;
            }
            
            var ratio = Mathf.InverseLerp(m_startTime, m_startTime + AnimationTime, Time.time);
            var animatedRatio = Animation.Evaluate(ratio);

            m_currentValue = Mathf.Lerp(m_startValue, m_destinationValue, animatedRatio);
            Set(m_currentValue);

            if (Time.time > m_startTime + AnimationTime) {
                m_animating = false;    
            }
        }

        private void BindValue(BasicValueModel value)
        {
            if (AnimationTime <= 0) {
                Set((float) value.AsDouble);
                return;
            }
            
            m_startTime = Time.time;
            
            if (m_hasReceivedValue) {
                m_startValue = m_currentValue;
                m_destinationValue = (float) value.AsDouble;    
            } else {
                m_currentValue = (float)value.AsDouble;
                m_startValue = m_currentValue;
                m_destinationValue = m_currentValue;
                m_hasReceivedValue = true;
            }
            
            Set(m_currentValue);

            m_animating = true;
        }

        private void Set(float value)
        {
            Rect.anchorMax = new Vector2(Mathf.Lerp(StartValue, EndValue, value), 1);
        }
        
        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }

        private void Reset()
        {
            Animation = AnimationCurve.Linear(0, 0, 0.25f, 1);
        }
    }
}