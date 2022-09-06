using System;
using Tekly.Common.Ui;
using Tekly.DataModels.Models;
using Tekly.Logging;

namespace Tekly.DataModels.Binders
{
    public class ButtonBinder : Binder
    {
        public ModelRef Key;
        public ButtonBase Button;
        public ButtonAction Action;
        
        private IDisposable m_disposable;
        private ButtonModel m_buttonModel;

        private void Awake()
        {
            if (ReferenceEquals(Button, null) == false) {
                Button.OnClick.AddListener(OnButtonClicked);    
            } else {
                TkLogger.Get<UnityButtonBinder>().ErrorContext("ButtonBinder has a null button", this);
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
            Button.Interactable = value;
        }

        private void OnDestroy()
        {
            m_disposable?.Dispose();
            m_buttonModel = null;
        }

        private void OnButtonClicked(ButtonBase _)
        {
            m_buttonModel.Activate();
            
            if (ReferenceEquals(Action, null) == false) {
                Action.Activate();
            }
        }
    }
}