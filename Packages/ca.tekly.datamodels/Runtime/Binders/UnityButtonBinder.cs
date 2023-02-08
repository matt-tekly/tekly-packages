using System;
using Tekly.DataModels.Models;
using Tekly.Logging;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Tekly.DataModels.Binders
{
    public class UnityButtonBinder : Binder
    {
        [FormerlySerializedAs("Key")] [SerializeField] private ModelRef m_key;
        [FormerlySerializedAs("Button")] [SerializeField] private Button m_button;
        [FormerlySerializedAs("Action")] [SerializeField] private ButtonAction m_action;
        
        private IDisposable m_disposable;
        private ButtonModel m_buttonModel;

        private void Awake()
        {
            if (ReferenceEquals(m_button, null) == false) {
                m_button.onClick.AddListener(OnButtonClicked);    
            } else {
                TkLogger.Get<UnityButtonBinder>().ErrorContext("UnityButtonBinder has a null button", this);
            }
        }

        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out m_buttonModel)) {
                m_disposable?.Dispose();
                m_disposable = m_buttonModel.Interactable.Subscribe(BindBool);
            }
        }

        private void BindBool(bool value)
        {
            m_button.interactable = value;
        }

        public override void UnBind()
        {
            m_disposable?.Dispose();
            m_buttonModel = null;
        }

        private void OnButtonClicked()
        {
            m_buttonModel.Activate();
            
            if (ReferenceEquals(m_action, null) == false) {
                m_action.Activate();
            }
        }
    }
}