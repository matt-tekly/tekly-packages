using System;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.DataModels.Binders
{
    /// <summary>
    /// Generically bind a bool model to a UnityEvent
    /// </summary>
    public class BoolEventBinder: Binder
    {
        [SerializeField] private ModelRef m_key;
        [SerializeField] private bool m_invert;
        [SerializeField] private UnityEvent<bool> m_event;
        
        private IDisposable m_disposable;

        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out BoolValueModel model)) {
                m_disposable?.Dispose();
                m_disposable = model.Subscribe(BindValue);
            }
        }

        private void BindValue(bool value)
        {
            if (m_event != null) {
                m_event.Invoke(value ^ m_invert);
            }
        }
        
        public override void UnBind()
        {
            m_disposable?.Dispose();
            m_disposable = null;
        }
    }
}