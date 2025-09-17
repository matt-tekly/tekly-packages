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
    
    public class VisibilityBinder : BasicBinder<IValueModel>
    {
        [SerializeField] private VisibilityTarget[] m_targets;

        protected override IDisposable Subscribe(IValueModel model)
        {
            BindValue(model.IsTruthy);
            return model.Modified.Subscribe(_ => BindValue(model.IsTruthy));
        }
        
        private void BindValue(bool value)
        {
            foreach (var target in m_targets) {
                Presentable.SetGameObjectActive(target.Target, value ^ target.Invert);
            }
        }
    }
}