using System;
using Tekly.DataModels.Models;

namespace Tekly.DataModels.Binders
{
    /// <summary>
    /// Takes in a Key and transforms that into another key which overrides the target BinderContainer's key
    /// </summary>
    public class BinderKeyProxy : Binder
    {
        public ModelRef Key;
        public BinderContainer Target;
        public string KeyFormat;
        
        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(Key.Path, out StringValueModel stringModel)) {
                m_disposable?.Dispose();
                m_disposable = stringModel.Subscribe(BindKey);
            }
        }

        private void BindKey(string value)
        {
            if (string.IsNullOrEmpty(KeyFormat)) {
                Target.OverrideKey(value);    
            } else {
                Target.OverrideKey(string.Format(KeyFormat, value));
            }
        }
        
        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
    }
}