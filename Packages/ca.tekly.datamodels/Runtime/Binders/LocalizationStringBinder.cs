using System;
using Tekly.Common.Observables;
using Tekly.DataModels.Models;
using Tekly.Localizations;
using Tekly.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class LocalizationStringBinder : Binder
    {
        [FormerlySerializedAs("LocalizationId")] [SerializeField] private string m_localizationId;
        [FormerlySerializedAs("Keys")] [SerializeField] private FormatAndDataKey[] m_keys;

        [FormerlySerializedAs("Text")] [SerializeField] private TMP_Text m_text;

        private (string, object)[] m_values;
        private IDisposable[] m_listeners;

        private bool m_canFormat;
        private readonly TkLogger m_logger = TkLogger.Get<LocalizationStringBinder>();
        
        public override void Bind(BinderContainer container)
        {
            Clear();

            Array.Resize(ref m_values, m_keys.Length);
            Array.Resize(ref m_listeners, m_keys.Length);
            
            for (var index = 0; index < m_keys.Length; index++) {
                m_values[index].Item1 = m_keys[index].FormatKey;
            }
            
            m_canFormat = false;
            
            for (var index = 0; index < m_keys.Length; index++) {
                var key = m_keys[index];

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

            m_text.text = Localizer.Instance.Localize(m_localizationId, m_values);
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

            m_listeners = null;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_text == null) {
                m_text = GetComponent<TMP_Text>();
            }
        }
#endif

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
            private readonly ITriggerable<T> m_triggerable;
            
            public Listener(LocalizationStringBinder owner, int index, ITriggerable<T> triggerable, (string, object)[] values)
            {
                m_owner = owner;
                m_index = index;
                m_values = values;
                
                if (triggerable != null) {
                    m_triggerable = triggerable;
                    m_disposable = triggerable.Subscribe(this);
                } else {
                    m_values[index].Item2 = "[NF]";
                }
            }
            
            void IValueObserver<T>.Changed(T value)
            {
                if (m_triggerable is StringValueModel stringValueModel) {
                    if (stringValueModel.NeedsLocalization) {
                        m_values[m_index].Item2= Localizer.Instance.Localize(stringValueModel.Value);
                    } else {
                        m_values[m_index].Item2 = value;   
                    }
                } else {
                    m_values[m_index].Item2 = value;  
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