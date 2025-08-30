using System;
using Tekly.DataModels.Models;
using Tekly.Logging;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Tekly.DataModels.Binders
{
    public class UnityButtonBinder : BasicBinder<ButtonModel>
    {
        [FormerlySerializedAs("Button")] [SerializeField] private Button m_button;
        [FormerlySerializedAs("Action")] [SerializeField] private ButtonAction m_action;
        
        private void Awake()
        {
            if (ReferenceEquals(m_button, null) == false) {
                m_button.onClick.AddListener(OnButtonClicked);    
            } else {
                TkLogger.Get<UnityButtonBinder>().ErrorContext("UnityButtonBinder has a null button", this);
            }
        }

        protected override IDisposable Subscribe(ButtonModel model)
        {
            return m_model.Interactable.Subscribe(BindBool);
        }

        private void BindBool(bool value)
        {
            m_button.interactable = value;
        }
        
        private void OnButtonClicked()
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
                m_button = GetComponent<Button>();
            }
        }
#endif
    }
}