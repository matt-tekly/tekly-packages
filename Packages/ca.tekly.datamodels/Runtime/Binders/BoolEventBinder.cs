using UnityEngine;
using UnityEngine.Events;

namespace Tekly.DataModels.Binders
{
    /// <summary>
    /// Generically bind a bool model to a UnityEvent
    /// </summary>
    public class BoolEventBinder : BasicValueBinder<bool>
    {
        [SerializeField] private bool m_invert;
        [SerializeField] private UnityEvent<bool> m_event;
        
        protected override void BindValue(bool value)
        {
            if (m_event != null) {
                m_event.Invoke(value ^ m_invert);
            }
        }
    }
}