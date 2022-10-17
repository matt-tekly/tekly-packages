using System;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.Common.Tweenimation.Tweens
{
    [Serializable]
    public class ActionTween : BaseTween
    {
        [SerializeField] private UnityEvent m_action;
        private bool m_activated;

        public override void Evaluate(float time)
        {
            if (!m_enabled && !m_activated && time > m_delay) {
                m_activated = true;
                m_action.Invoke();
            }
        }

        protected override void OnEvaluate(float ratio)
        {
            
        }
        
        protected override void OnPlay(bool reinitialize)
        {
            m_activated = false;
        }
    }
}