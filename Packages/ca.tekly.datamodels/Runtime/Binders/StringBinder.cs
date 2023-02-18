using System;
using Tekly.DataModels.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class StringBinder : Binder
    {
        [FormerlySerializedAs("Key")] [SerializeField] private ModelRef m_key;
        [FormerlySerializedAs("Text")] [SerializeField] private TMP_Text m_text;

        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out StringValueModel stringModel)) {
                m_disposable?.Dispose();
                m_disposable = stringModel.Subscribe(BindString);
            }
        }

        private void BindString(string value)
        {
            m_text.text = value;
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_disposable?.Dispose();
        }
    }
}