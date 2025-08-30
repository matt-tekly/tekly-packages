using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class CountdownBinder : BasicValueBinder<double>
    {
        [FormerlySerializedAs("Text")] [SerializeField] private TMP_Text m_text;
        [FormerlySerializedAs("Format")] [SerializeField] private string m_format = "c";
        
        private DateTime m_endTime;
        
        private void Update()
        {
            var now = DateTime.UtcNow;
            var span = m_endTime - now;
            
            m_text.text = span.ToString(m_format);
        }

        protected override void BindValue(double value)
        {
            m_endTime = DateTime.FromOADate(value);
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