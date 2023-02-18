using System;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    /// <summary>
    /// Takes in a Key and transforms that into another key which overrides the target BinderContainer's key
    /// </summary>
    public class BinderKeyProxy : Binder
    {
        [FormerlySerializedAs("Key")] [SerializeField] private ModelRef m_key;
        [FormerlySerializedAs("Target")] [SerializeField] private BinderContainer m_target;
        [FormerlySerializedAs("KeyFormat")] [SerializeField] private string m_keyFormat;
        
        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out StringValueModel stringModel)) {
                m_disposable?.Dispose();
                m_disposable = stringModel.Subscribe(BindKey);
            }
        }

        private void BindKey(string value)
        {
            if (string.IsNullOrEmpty(m_keyFormat)) {
                m_target.OverrideKey(value);    
            } else {
                m_target.OverrideKey(string.Format(m_keyFormat, value));
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_disposable?.Dispose();
        }
    }
}