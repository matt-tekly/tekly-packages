using System;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.DataModels.Binders
{
    /// <summary>
    /// Generically bind a string model to a UnityEvent
    /// </summary>
    public class StringEventBinder: Binder
    {
        [SerializeField] private ModelRef m_key;
        [SerializeField] private UnityEvent<string> m_event;

        private IDisposable m_disposable;

        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out StringValueModel model)) {
                m_disposable?.Dispose();
                m_disposable = model.Subscribe(BindValue);
            }
        }

        private void BindValue(string value)
        {
            if (m_event != null) {
                m_event.Invoke(value);
            }
        }

        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
    }
}