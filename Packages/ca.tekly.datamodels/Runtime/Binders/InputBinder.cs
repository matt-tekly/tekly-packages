using System;
using Tekly.DataModels.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class InputBinder : Binder
    {
        [FormerlySerializedAs("Key")] [SerializeField] private ModelRef m_key;
        [FormerlySerializedAs("InputField")] [SerializeField] private TMP_InputField m_inputField;

        private StringValueModel m_model;
        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out m_model)) {
                m_disposable?.Dispose();
                m_disposable = m_model.Subscribe(BindString);
            }
        }

        private void BindString(string value)
        {
            m_inputField.SetTextWithoutNotify(value);
        }

        private void Awake()
        {
            m_inputField.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(string value)
        {
            m_model.Value = value;
        }
    }
}