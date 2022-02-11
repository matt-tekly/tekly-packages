using Tekly.Common.Observables;
using UnityEngine;

namespace Tekly.Common.Variables
{
 
    [CreateAssetMenu(menuName = "Game/Variables/Bool")]
    public class BoolVariable : ScriptableVariable
    {
        public bool Current { get; private set; }
        
        public ITriggerable<BoolVariable> Observable => m_triggerable;
        
        [SerializeField] private bool m_default;
        
        private readonly Triggerable<BoolVariable> m_triggerable = new Triggerable<BoolVariable>();

        protected override void OnInit()
        {
            Current = m_prefsContainer.GetBool(name, m_default);
        }
        
        public void Set(bool value)
        {
            Current = value;
            m_prefsContainer.Set(name, Current);
            MarkDirty();
            ForceEmit();
        }

        public override void ForceEmit()
        {
            m_triggerable.Emit(this);
        }
    }
}