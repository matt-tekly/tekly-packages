using System;
using Tekly.Common.Ui;
using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public class ProgressBarBinder : Binder
    {
        [SerializeField] private ProgressBar m_progressBar;

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
            m_progressBar.SetProgress(value);
        }

        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
    }
}