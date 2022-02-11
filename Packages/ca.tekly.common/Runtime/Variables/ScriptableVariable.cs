using System;
using Tekly.Common.LocalPrefs;
using UnityEngine;

namespace Tekly.Common.Variables
{
    public class ScriptableVariable : ScriptableObject
    {
        [NonSerialized] private ScriptableVariableContainer m_container;

        protected PrefsContainer m_prefsContainer;
        
        public void Initialize(ScriptableVariableContainer container, PrefsContainer prefsContainer)
        {
            m_container = container;
            m_prefsContainer = prefsContainer;
            
            OnInit();
        }
        
        public virtual void ForceEmit() {}
        
        protected void MarkDirty()
        {
            m_container.VariableChanged(this);
        }

        protected virtual void OnInit()
        {
            
        }
    }
}