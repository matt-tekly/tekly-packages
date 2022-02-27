// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using Tekly.Common.Observables;
using Tekly.DataModels.Models;
using Tekly.Logging;
using TMPro;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public class FormattedStringBinder : Binder
    {
        public ModelRef[] Keys;
        
        [TextArea] public string Format;
        
        public TMP_Text Text;

        private object[] m_values;
        private IDisposable[] m_listeners;

        private bool m_canFormat;
        private readonly TkLogger m_logger = TkLogger.Get<FormattedStringBinder>();
        
        public override void Bind(BinderContainer container)
        {
            if (m_values == null) {
                m_values = new object[Keys.Length];
                m_listeners = new IDisposable[Keys.Length];
            } else {
                Dispose();
            }
            
            m_canFormat = false;
            
            for (var index = 0; index < Keys.Length; index++) {
                var key = Keys[index].Path;

                if (!container.TryGet(key, out IValueModel model)) {
                    m_logger.ErrorContext("Failed to find Model: [{key}]", this, ("key", key));
                }

                switch (model) {
                    case BoolValueModel valueModel:
                        m_listeners[index] = Listener<bool>.Create(this, index, valueModel, m_values);
                        break;
                    case StringValueModel valueModel:
                        m_listeners[index] = Listener<string>.Create(this, index, valueModel, m_values);
                        break;
                    case NumberValueModel valueModel:
                        m_listeners[index] = Listener<double>.Create(this, index, valueModel, m_values);
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
            
            Text.text = string.Format(Format, m_values);
        }
        
        private void OnDestroy()
        {
            Dispose();
        }

        private void Dispose()
        {
            if (m_listeners != null) {
                foreach (var disposable in m_listeners) {
                    disposable.Dispose();
                }
            }
        }

        private class Listener<T> : IValueObserver<T>, IDisposable
        {
            private FormattedStringBinder m_owner;
            private IDisposable m_disposable;
        
            private int m_index;
            private object[] m_values;
            
            public void Changed(T value)
            {
                m_values[m_index] = value;
                m_owner.FormatString();
            }

            public static IDisposable Create(FormattedStringBinder owner, int index, ValueModel<T> valueModel, object[] values)
            {
                Listener<T> listener = new Listener<T>();
                listener.m_owner = owner;
                listener.m_index = index;
                listener.m_values = values;
                
                if (valueModel != null) {
                    listener.m_disposable = valueModel.Subscribe(listener);
                } else {
                    listener.m_values[index] = "[NM]";
                }
                
                return listener;
            }

            public void Dispose()
            {
                m_disposable?.Dispose();
            }
        }
    }
}