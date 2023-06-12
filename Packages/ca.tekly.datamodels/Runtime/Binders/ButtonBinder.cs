using System;
using Tekly.Common.Ui;
using Tekly.DataModels.Models;
using Tekly.Logging;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class ButtonBinder : Binder
    {
        [FormerlySerializedAs("Key")] [SerializeField] private ModelRef m_key;
        [FormerlySerializedAs("Button")] [SerializeField] private ButtonBase m_button;
        [FormerlySerializedAs("Action")] [SerializeField] private ButtonAction m_action;
        
        private IDisposable m_disposable;
        private ButtonModel m_buttonModel;

        private void Awake()
        {
            if (ReferenceEquals(m_button, null) == false) {
                m_button.OnClick.AddListener(OnButtonClicked);    
            } else {
                TkLogger.Get<UnityButtonBinder>().ErrorContext("ButtonBinder has a null button", this);
            }
        }

        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out m_buttonModel)) {
                m_disposable?.Dispose();
                m_disposable = m_buttonModel.Interactable.Subscribe(BindBool);
            }
        }
        
        public override void UnBind()
        {
            m_disposable?.Dispose();
            m_buttonModel = null;
        }

        private void BindBool(bool value)
        {
            m_button.Interactable = value;
        }

        private void OnButtonClicked(ButtonBase _)
        {
            m_buttonModel.Activate();
            
            if (ReferenceEquals(m_action, null) == false) {
                m_action.Activate();
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_button == null) {
                m_button = GetComponent<ButtonBase>();
            }
        }
#endif
    }
}