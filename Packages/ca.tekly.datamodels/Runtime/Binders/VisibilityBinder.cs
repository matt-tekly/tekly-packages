using System;
using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public class VisibilityBinder : Binder
    {
        public ModelRef Key;
        
        public bool Invert;
        
        public GameObject[] Targets;

        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(Key.Path, out BoolValueModel stringModel)) {
                m_disposable?.Dispose();
                m_disposable = stringModel.Subscribe(BindString);
            }
        }

        private void BindString(BasicValueModel value)
        {
            var active = value.AsBool != Invert;
            
            foreach (var target in Targets) {
                target.SetActive(active);
            }
        }
        
        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
    }
}