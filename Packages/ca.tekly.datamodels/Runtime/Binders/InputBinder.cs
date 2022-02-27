using System;
using Tekly.DataModels.Models;
using TMPro;

namespace Tekly.DataModels.Binders
{
    public class InputBinder : Binder
    {
        public ModelRef Key;
        public TMP_InputField InputField;

        private StringValueModel m_model;
        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(Key.Path, out m_model)) {
                m_disposable?.Dispose();
                m_disposable = m_model.Subscribe(BindString);
            }
        }

        private void BindString(string value)
        {
            InputField.SetTextWithoutNotify(value);
        }

        private void Awake()
        {
            InputField.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(string value)
        {
            m_model.Value = value;
        }
    }
}