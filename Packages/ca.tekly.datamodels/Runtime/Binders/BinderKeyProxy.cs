using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    /// <summary>
    /// Takes in a Key and transforms that into another key which overrides the target BinderContainer's key
    /// </summary>
    public class BinderKeyProxy : BasicValueBinder<string>
    {
        [FormerlySerializedAs("Target")] [SerializeField] private BinderContainer m_target;
        [FormerlySerializedAs("KeyFormat")] [SerializeField] private string m_keyFormat;
        
        protected override void BindValue(string value)
        {
            if (string.IsNullOrEmpty(m_keyFormat)) {
                m_target.OverrideKey(value);    
            } else {
                m_target.OverrideKey(string.Format(m_keyFormat, value));
            }
        }
    }
}