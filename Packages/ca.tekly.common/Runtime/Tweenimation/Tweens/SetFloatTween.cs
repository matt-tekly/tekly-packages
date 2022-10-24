using System;
using Tekly.Common.Maths;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.Common.Tweenimation.Tweens
{
    [Serializable]
    public class SetFloatTween : BasicTween
    {
        [SerializeField] private FloatRange m_range;
        [SerializeField] private UnityEvent<float> m_setter;
		
        protected override void OnEvaluate(float ratio)
        {
            m_setter.Invoke(m_range.Lerp(ratio));
        }
    }
}