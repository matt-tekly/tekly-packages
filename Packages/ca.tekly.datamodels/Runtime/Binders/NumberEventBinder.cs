using System;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.DataModels.Binders
{
    /// <summary>
    /// Generically bind a number model to a UnityEvent
    /// </summary>
    public class NumberEventBinder: Binder
    {
        [SerializeField] private ModelRef m_key;
        [SerializeField] private UnityEvent<float> m_event;
        
        private IDisposable m_disposable;

        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out NumberValueModel model)) {
                m_disposable?.Dispose();
                m_disposable = model.Subscribe(BindValue);
            }
        }

        private void BindValue(double value)
        {
            if (m_event != null) {
                m_event.Invoke((float)value);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_disposable?.Dispose();
        }
    }
}