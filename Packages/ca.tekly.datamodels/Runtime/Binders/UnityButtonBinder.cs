// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using Tekly.DataModels.Models;
using Tekly.Logging;
using UnityEngine.UI;

namespace Tekly.DataModels.Binders
{
    public class UnityButtonBinder : Binder
    {
        public ModelRef Key;
        public Button Button;
        public ButtonAction Action;
        
        private IDisposable m_disposable;
        private ButtonModel m_buttonModel;

        private void Awake()
        {
            if (ReferenceEquals(Button, null) == false) {
                Button.onClick.AddListener(OnButtonClicked);    
            } else {
                TkLogger.Get<UnityButtonBinder>().ErrorContext("UnityButtonBinder has a null button", this);
            }
        }

        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(Key.Path, out m_buttonModel)) {
                m_disposable?.Dispose();
                m_disposable = m_buttonModel.Interactable.Subscribe(BindBool);
            }
        }

        private void BindBool(bool value)
        {
            Button.interactable = value;
        }

        private void OnDestroy()
        {
            m_disposable?.Dispose();
            m_buttonModel = null;
        }

        private void OnButtonClicked()
        {
            m_buttonModel.Activate();
            
            if (ReferenceEquals(Action, null) == false) {
                Action.Activate();
            }
        }
    }
}