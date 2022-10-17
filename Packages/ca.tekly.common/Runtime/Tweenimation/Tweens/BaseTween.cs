using System;
using UnityEngine;

namespace Tekly.Common.Tweenimation.Tweens
{
    [Serializable]
    public abstract class BaseTween : ISerializationCallbackReceiver
    {
        [HideInInspector] [SerializeField] private string m_name;
        [HideInInspector] [SerializeField] protected bool m_enabled = true;
        [SerializeField] protected float m_delay;
        
        protected Tweenimator m_context;
        
        public virtual float TotalTime => m_delay;
        
        public void Initialize(Tweenimator context)
        {
            m_context = context;
            OnInitialize();
        }
        
        public void Play(bool reinitialize = false)
        {
            OnPlay(reinitialize);
            
            if (m_enabled) {
                OnEvaluate(0);    
            }
        }

        protected virtual void OnInitialize() { }

        protected virtual void OnPlay(bool reinitialize) { }

        public abstract void Evaluate(float time);

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_name)) {
                m_name = GetType().Name;
            }
        }
        
        protected abstract void OnEvaluate(float ratio);
    }
}