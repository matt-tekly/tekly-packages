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
        private Listener[] m_listeners;

        private bool m_canFormat;
        private readonly TkLogger m_logger = TkLogger.Get<LocalizationStringBinder>();
        
        public override void Bind(BinderContainer container)
        {
            if (m_values == null) {
                m_values = new (string, object)[Keys.Length];
                for (var index = 0; index < Keys.Length; index++) {
                    m_values[index].Item1 = Keys[index].FormatKey;
                }

                m_listeners = new Listener[Keys.Length];
            } else {
                Dispose();
            }
            
            m_canFormat = false;
            
            for (var index = 0; index < Keys.Length; index++) {
                var key = Keys[index];

                if (!container.TryGet(key.ModelKey.Path, out BasicValueModel model)) {
                    m_logger.ErrorContext("Failed to find Model: [{key}]", this, ("key", key.ModelKey));
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

            Text.text = Localizer.Instance.Localize(LocalizationId, m_values);
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

        [Serializable]
        public class FormatAndDataKey
        {
            public string FormatKey;
            public ModelRef ModelKey;
        }
        
        private class Listener : IValueObserver<BasicValueModel>, IDisposable
        {
            private LocalizationStringBinder m_owner;
            private IDisposable m_disposable;
        
            private int m_index;
            private (string, object)[] m_values;
            
            public void Changed(BasicValueModel value)
            {
                m_values[m_index].Item2 = value.AsObject;
                m_owner.FormatString();
            }

            public static Listener Create(LocalizationStringBinder owner, int index, BasicValueModel valueModel, (string, object)[] values)
            {
                Listener listener = new Listener();
                listener.m_owner = owner;
                listener.m_index = index;
                listener.m_values = values;
                
                if (valueModel != null) {
                    listener.m_disposable = valueModel.Subscribe(listener);
                } else {
                    listener.m_values[index].Item2 = "[NF]";
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