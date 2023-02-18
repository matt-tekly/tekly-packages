using System;
using Tekly.Common.Ui.ProgressBars;
using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public class ProgressBarBinder : Binder
    {
        [SerializeReference] private Filled m_filled;

        [SerializeField] private ModelRef m_key;

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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_disposable?.Dispose();
        }
    }
}