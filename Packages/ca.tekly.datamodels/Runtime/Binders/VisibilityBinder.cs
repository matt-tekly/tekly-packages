using System;
using Tekly.Common.Presentables;
using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    [Serializable]
    public struct VisibilityTarget
    {
        public GameObject Target;
        public bool Invert;
    }
    
    public class VisibilityBinder : Binder
    {
        [SerializeField] private ModelRef m_key;
        [SerializeField] private VisibilityTarget[] m_targets;

        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out BoolValueModel stringModel)) {
                m_disposable?.Dispose();
                m_disposable = stringModel.Subscribe(BindBool);
            }
        }
        
        public override void UnBind()
        {
            m_disposable?.Dispose();
        }

        private void BindBool(bool value)
        {
            foreach (var target in m_targets) {
                var go = target.Target;
                if (go.TryGetComponent(out Presentable presentable)) {
                    presentable.Present(value ^ target.Invert);
                } else {
                    go.SetActive(value ^ target.Invert);
                }
            }
        }
    }
}