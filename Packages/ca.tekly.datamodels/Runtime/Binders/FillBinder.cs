using System;
using Tekly.Common.Ui.ProgressBars;
using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public class FillBinder : Binder
    {
        [SerializeField] private ModelRef m_key;
        [SerializeField] private Filled m_filled;

        private IDisposable m_disposable;

        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out NumberValueModel numberValue)) {
                m_disposable?.Dispose();
                m_disposable = numberValue.Subscribe(BindValue);
            }
        }

        private void BindValue(double value)
        {
            m_filled.Fill = (float) value;
        }

        public override void UnBind()
        {
            m_disposable?.Dispose();
            m_disposable = null;
        }
    }
}