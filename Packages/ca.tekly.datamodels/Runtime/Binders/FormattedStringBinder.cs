// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using Tekly.Common.Observables;
using Tekly.DataModels.Models;
using Tekly.Localizations;
using Tekly.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Tekly.DataModels.Binders
{
    public class FormattedStringBinder : Binder
    {
        
        public TextSetEvent OnTextSet => m_onTextSet;
        
        [FormerlySerializedAs("Keys")] [SerializeField] private ModelRef[] m_keys;

        [FormerlySerializedAs("Format")] [SerializeField] [TextArea] private string m_format;

        [FormerlySerializedAs("Text")] [SerializeField] private TMP_Text m_text;
        
        [SerializeField] private TextSetEvent m_onTextSet = new TextSetEvent();
        
        private object[] m_values;
        private IDisposable[] m_listeners;

        private bool m_canFormat;
        private readonly TkLogger m_logger = TkLogger.Get<FormattedStringBinder>();
        
        public override void Bind(BinderContainer container)
        {
            Clear();

            Array.Resize(ref m_values, m_keys.Length);
            Array.Resize(ref m_listeners, m_keys.Length);
            
            m_canFormat = false;

            for (var index = 0; index < m_keys.Length; index++) {
                var key = m_keys[index].Path;

                if (!container.TryGet(key, out IValueModel model)) {
                    m_logger.ErrorContext("Failed to find Model: [{key}]", this, ("key", key));
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

            var text = string.Format(m_format, m_values);
            
            if (m_text != null) {
                m_text.text = text;    
            }
            
            m_onTextSet.Invoke(text);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
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
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_text == null) {
                m_text = GetComponent<TMP_Text>();
            }
        }
#endif

        private class Listener<T> : IValueObserver<T>, IDisposable
        {
            private readonly FormattedStringBinder m_owner;
            private readonly IDisposable m_disposable;

            private readonly int m_index;
            private readonly object[] m_values;
            private readonly ITriggerable<T> m_triggerable;

            public Listener(FormattedStringBinder owner, int index, ITriggerable<T> triggerable, object[] values)
            {
                m_owner = owner;
                m_index = index;
                m_values = values;

                if (triggerable != null) {
                    m_triggerable = triggerable;
                    m_disposable = triggerable.Subscribe(this);
                } else {
                    m_values[index] = "[NM]";
                }
            }

            void IValueObserver<T>.Changed(T value)
            {
                if (m_triggerable is StringValueModel stringValueModel) {
                    if (stringValueModel.NeedsLocalization) {
                        m_values[m_index] = Localizer.Instance.Localize(stringValueModel.Value);
                    } else {
                        m_values[m_index] = value;    
                    }
                } else {
                    m_values[m_index] = value;    
                }
                
                m_owner.FormatString();
            }

            public void Dispose()
            {
                m_disposable?.Dispose();
            }
        }
    }
}