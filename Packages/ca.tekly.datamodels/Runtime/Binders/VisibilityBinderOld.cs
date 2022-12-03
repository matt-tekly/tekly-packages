using System;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class VisibilityBinderOld : Binder
    {
        [FormerlySerializedAs("Key")] [SerializeField] private ModelRef m_key;
        
        [FormerlySerializedAs("Invert")] [SerializeField] private bool m_invert;
        
        [FormerlySerializedAs("Targets")] [SerializeField] private GameObject[] m_targets;

        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out BoolValueModel stringModel)) {
                m_disposable?.Dispose();
                m_disposable = stringModel.Subscribe(BindBool);
            }
        }

        private void BindBool(bool value)
        {
            var active = value ^ m_invert;
            
            foreach (var target in m_targets) {
                target.SetActive(active);
            }
        }
        
        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
    }
}