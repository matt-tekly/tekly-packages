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
        private Listener[] m_listeners;

        private bool m_canFormat;
        private readonly TkLogger m_logger = TkLogger.Get<FormattedStringBinder>();
        
        public override void Bind(BinderContainer container)
        {
            if (m_values == null) {
                m_values = new object[Keys.Length];
                m_listeners = new Listener[Keys.Length];
            } else {
                Dispose();
            }
            
            m_canFormat = false;
            
            for (var index = 0; index < Keys.Length; index++) {
                var key = Keys[index].Path;

                if (!container.TryGet(key, out BasicValueModel model)) {
                    m_logger.ErrorContext("Failed to find Model: [{key}]", this, ("key", key));
                }
                
                m_listeners[index] = Listener.Create(this, index, model, m_values);
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

        private class Listener : IValueObserver<BasicValueModel>, IDisposable
        {
            private FormattedStringBinder m_owner;
            private IDisposable m_disposable;
        
            private int m_index;
            private object[] m_values;
            
            public void Changed(BasicValueModel value)
            {
                m_values[m_index] = value.AsObject;
                m_owner.FormatString();
            }

            public static Listener Create(FormattedStringBinder owner, int index, BasicValueModel valueModel, object[] values)
            {
                Listener listener = new Listener();
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