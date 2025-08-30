using System;
using Tekly.Common.Ui;
using Tekly.DataModels.Models;
using Tekly.Logging;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class ButtonBinder : BasicBinder<ButtonModel>
    {
        [FormerlySerializedAs("Button")] [SerializeField] private ButtonBase m_button;
        [FormerlySerializedAs("Action")] [SerializeField] private ButtonAction m_action;
        
        private void Awake()
        {
            if (ReferenceEquals(m_button, null) == false) {
                m_button.OnClick.AddListener(OnButtonClicked);    
            } else {
                TkLogger.Get<UnityButtonBinder>().ErrorContext("ButtonBinder has a null button", this);
            }
        }
        
        protected override IDisposable Subscribe(ButtonModel model)
        {
            return m_model.Interactable.Subscribe(BindBool);
        }
        
        private void BindBool(bool value)
        {
            m_button.Interactable = value;
        }

        private void OnButtonClicked(ButtonBase _)
        {
            m_model.Activate();
            
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