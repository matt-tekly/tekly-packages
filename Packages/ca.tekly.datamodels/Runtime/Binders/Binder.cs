using System;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public abstract class Binder : MonoBehaviour
    {
        public virtual void Bind(BinderContainer container)
        {
            
        }
        
        public virtual void UnBind()
        {
            
        }

        public virtual string ResolveFullKey(string key)
        {
            var container = GetComponentInParent<BinderContainer>();
            return container == null ? key : container.ResolveFullKey(key);
        }

        protected virtual void OnDestroy()
        {
            UnBind();
        }

        protected virtual void OnDisable()
        {
            
        }
    }

    public abstract class BasicBinder<T> : Binder where T : class, IModel
    {
        [FormerlySerializedAs("Key")][SerializeField] private ModelRef m_key;
        private IDisposable m_disposable;
        protected T m_model;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out m_model)) {
                m_disposable?.Dispose();
                m_disposable = Subscribe(m_model);
            }
        }

        protected abstract IDisposable Subscribe(T model);
        
        public override void UnBind()
        {
            m_disposable?.Dispose();
            m_disposable = null;
        }
    }
    
    public abstract class BasicValueBinder<T> : Binder where T : IEquatable<T>, IComparable<T>
    {
        [FormerlySerializedAs("Key")][SerializeField] private ModelRef m_key;
        private IDisposable m_disposable;
        protected BasicValueModel<T> m_model;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out m_model)) {
                m_disposable?.Dispose();
                m_disposable = m_model.Subscribe(BindValue);
            }
        }

        protected abstract void BindValue(T value);
        
        public override void UnBind()
        {
            m_disposable?.Dispose();
            m_disposable = null;
        }
    }
}