using System;
using Tekly.Common.Presentables;
using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    [Serializable]
    public struct VisibilityStringTarget
    {
        public GameObject Target;
        public string Value;
        public bool Invert;
    }
    
    public class VisibilityStringBinder : BasicValueBinder<string>
    {
        [SerializeField] private VisibilityStringTarget[] m_targets;
        
        protected override void BindValue(string value)
        {
            foreach (var target in m_targets) {
                var active = string.Equals(value, target.Value, StringComparison.OrdinalIgnoreCase);
                Presentable.SetGameObjectActive(target.Target, active ^ target.Invert);
            }
        }
    }
}