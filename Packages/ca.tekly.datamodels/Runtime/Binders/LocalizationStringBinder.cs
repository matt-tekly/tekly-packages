using System;
using Tekly.Common.Observables;
using Tekly.DataModels.Models;
using Tekly.Localizations;
using Tekly.Logging;
using TMPro;

namespace Tekly.DataModels.Binders
{
    public class LocalizationStringBinder: Binder
    {
        public string LocalizationId;
        public FormatAndDataKey[] Keys;

        public TMP_Text Text;

        private (string, object)[] m_values;
        private IDisposable[] m_listeners;

        private bool m_canFormat;
        private readonly TkLogger m_logger = TkLogger.Get<LocalizationStringBinder>();
        
        public override void Bind(BinderContainer container)
        {
            Clear();

            Array.Resize(ref m_values, Keys.Length);
            Array.Resize(ref m_listeners, Keys.Length);
            
            for (var index = 0; index < Keys.Length; index++) {
                m_values[index].Item1 = Keys[index].FormatKey;
            }
            
            m_canFormat = false;
            
            for (var index = 0; index < Keys.Length; index++) {
                var key = Keys[index];

                if (!container.TryGet(key.ModelKey.Path, out IValueModel model)) {
                    m_logger.ErrorContext("Failed to find Model: [{key}]", this, ("key", key.ModelKey));
                }
                
                switch (model) {
                    case BoolValueModel valueModel:
                        m_listeners[index] = new Listener<bool>(this, index, valueModel, m_values);
                        break;
                    case StringValueModel valueModel:
                        m_listeners[index] = new Listener<string>(this, index, valueModel, m_values);
                        break;
                    case NumberValueModel valueModel:
                        m_listeners[index] = new Listener<double>(this, index, valueModel, m_values);
                        break;
                    default:
                        m_logger.ErrorContext("Unsupported Model Type", this);
                        break;
                }
            }

            m_canFormat = true;
            FormatString();
        }
        
        private void FormatString()
        {
            if (!m_canFormat) {
                return;
            }

            Text.text = Localizer.Instance.Localize(LocalizationId, m_values);
        }
        
        private void OnDestroy()
        {
            Dispose();
        }

        private void Dispose()
        {
            Clear();
            
            m_listeners = null;
            m_values = null;
        }
        
        private void Clear()
        {
            if (m_listeners == null) {
                return;
            }
            
            foreach (var disposable in m_listeners) {
                disposable.Dispose();
            }
        }

        [Serializable]
        public class FormatAndDataKey
        {
            public string FormatKey;
            public ModelRef ModelKey;
        }
        
        private class Listener<T> : IValueObserver<T>, IDisposable
        {
            private readonly LocalizationStringBinder m_owner;
            private readonly IDisposable m_disposable;
        
            private readonly int m_index;
            private readonly (string, object)[] m_values;
            
            public Listener(LocalizationStringBinder owner, int index, ITriggerable<T> triggerable, (string, object)[] values)
            {
                m_owner = owner;
                m_index = index;
                m_values = values;
                
                if (triggerable != null) {
                    m_disposable = triggerable.Subscribe(this);
                } else {
                    m_values[index].Item2 = "[NF]";
                }
            }
            
            void IValueObserver<T>.Changed(T value)
            {
                m_values[m_index].Item2 = value;
                m_owner.FormatString();
            }

            public void Dispose()
            {
                m_disposable?.Dispose();
            }
        }
    }
}