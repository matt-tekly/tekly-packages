using System;
using Tekly.Common.Presentables;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    [Serializable]
    public struct VisibilityTarget
    {
        public GameObject Target;
        public bool Invert;
    }
    
    public class VisibilityBinder : BasicValueBinder<bool>
    {
        [SerializeField] private VisibilityTarget[] m_targets;

        protected override void BindValue(bool value)
        {
            foreach (var target in m_targets) {
                var go = target.Target;
                var isActive = value ^ target.Invert;
                
                if (go.TryGetComponent(out Presentable presentable)) {
                    presentable.Present(isActive);
                } else {
                    go.SetActive(isActive);
                }
            }
        }
    }
}