using System;
using Tekly.DataModels.Models;
using TMPro;

namespace Tekly.DataModels.Binders
{
    public class CountdownBinder : Binder
    {
        public ModelRef Key;
        public TMP_Text Text;
        public string Format = "c";
        
        private IDisposable m_disposable;
        private DateTime m_endTime;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(Key.Path, out NumberValueModel numberValue)) {
                m_disposable?.Dispose();
                m_disposable = numberValue.Subscribe(BindValue);
            }
        }

        private void Update()
        {
            var now = DateTime.UtcNow;
            var span = m_endTime - now;
            
            Text.text = span.ToString(Format);
        }

        private void BindValue(BasicValueModel value)
        {
            m_endTime = DateTime.FromOADate(value.AsDouble);
        }
        
        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
    }
}