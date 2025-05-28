using System;
using Tekly.Common.Maths;
using Tekly.DataModels.Models;
using TMPro;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public class CounterBinder : Binder
    {
        public ModelRef Key;
        public TMP_Text Text;
        public string Format = "N0";

        public float AnimationTime;
        public AnimationCurve Animation;
        
        private IDisposable m_disposable;

        private float m_startTime;
        
        private double m_startValue;
        private double m_currentValue;
        private double m_destinationValue;

        private bool m_animating;
        
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

            m_currentValue = MathUtils.Lerp(m_startValue, m_destinationValue, animatedRatio);
            Text.text = m_currentValue.ToString(Format);

            if (Time.time > m_startTime + AnimationTime) {
                m_animating = false;    
            }
        }

        private void BindValue(double value)
        {
            m_startTime = Time.time;
            m_startValue = m_currentValue;
            m_destinationValue = value;
            
            Text.text = m_currentValue.ToString(Format);

            m_animating = true;
        }
        
        public override void UnBind()
        {
            m_disposable?.Dispose();
            m_disposable = null;
        }
    }
}