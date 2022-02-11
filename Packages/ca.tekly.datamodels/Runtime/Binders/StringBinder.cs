// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using Tekly.DataModels.Models;
using TMPro;

namespace Tekly.DataModels.Binders
{
    public class StringBinder : Binder
    {
        public ModelRef Key;
        public TMP_Text Text;

        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(Key.Path, out StringValueModel stringModel)) {
                m_disposable?.Dispose();
                m_disposable = stringModel.Subscribe(BindString);
            }
        }

        private void BindString(BasicValueModel value)
        {
            Text.text = value.AsString;
        }
        
        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
    }
}