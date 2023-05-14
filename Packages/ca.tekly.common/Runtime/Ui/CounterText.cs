using System;
using Tekly.Common.Maths;
using Tekly.Common.Tweenimation;
using Tekly.Common.Utils.Tweening;
using TMPro;
using UnityEngine;

namespace Tekly.Common.Ui
{
    public class CounterText : MonoBehaviour
    {
        [SerializeField] private string m_format = "N0";
        [SerializeField] private TMP_Text m_text;

        [SerializeField] private float m_animationTime = .5f;
        [SerializeField] private EaseData m_ease;

        public float AnimationTime {
            get => m_animationTime;
            set => m_animationTime = value;
        }

        public bool IsAnimating => m_animating;
        
        private IDisposable m_disposable;

        private float m_startTime;
        
        private double m_startValue;
        private double m_currentValue;
        private double m_destinationValue;

        private bool m_animating;

        public void SetValue(float value)
        {
            SetValue((double) value);
        }

        public void SetValue(double value)
        {
            m_startTime = Time.time;
            m_startValue = m_currentValue;
            m_destinationValue = value;
            
            m_text.text = m_currentValue.ToString(m_format);

            m_animating = true;
        }
        
        public void SetValueInstantly(double value)
        {
            m_startValue = value;
            m_destinationValue = value;
            m_currentValue = value;
            
            m_text.text = m_currentValue.ToString(m_format);

            m_animating = false;
        }

        private void Update()
        {
            if (!m_animating) {
                return;
            }

            var ratio = Mathf.InverseLerp(m_startTime, m_startTime + m_animationTime, Time.time);
            var animatedRatio = m_ease.Evaluate(ratio);

            m_currentValue = MathUtils.Lerp(m_startValue, m_destinationValue, animatedRatio);
            m_text.text = m_currentValue.ToString(m_format);

            if (Time.time > m_startTime + m_animationTime) {
                m_animating = false;    
            }
        }
        
        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_text == null) {
                m_text = GetComponent<TMP_Text>();
            }
        }
#endif
    }
}