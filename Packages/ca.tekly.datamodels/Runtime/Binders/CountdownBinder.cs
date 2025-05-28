using System;
using Tekly.DataModels.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class CountdownBinder : Binder
    {
        [FormerlySerializedAs("Key")] [SerializeField] private ModelRef m_key;
        [FormerlySerializedAs("Text")] [SerializeField] private TMP_Text m_text;
        [FormerlySerializedAs("Format")] [SerializeField] private string m_format = "c";
        
        private IDisposable m_disposable;
        private DateTime m_endTime;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out NumberValueModel numberValue)) {
                m_disposable?.Dispose();
                m_disposable = numberValue.Subscribe(BindValue);
            }
        }

        private void Update()
        {
            var now = DateTime.UtcNow;
            var span = m_endTime - now;
            
            m_text.text = span.ToString(m_format);
        }

        private void BindValue(double value)
        {
            m_endTime = DateTime.FromOADate(value);
        }
        
        public override void UnBind()
        {
            m_disposable?.Dispose();
            m_disposable = null;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_text == null) {
                m_text = GetComponent<TMP_Text>();
            }
        }
#endif
    }
}