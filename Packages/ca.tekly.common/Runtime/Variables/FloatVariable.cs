using Tekly.Common.Observables;
using UnityEngine;

namespace Tekly.Common.Variables
{
    [CreateAssetMenu(menuName = "Game/Variables/Float")]
    public class FloatVariable : ScriptableVariable
    {
        public float Current { get; private set; }
        
        public ITriggerable<FloatVariable> Observable => m_triggerable;
        
        [SerializeField] private float m_default;
        
        private readonly Triggerable<FloatVariable> m_triggerable = new Triggerable<FloatVariable>();

        protected override void OnInit()
        {
            Current = m_prefsContainer.GetFloat(name, m_default);
        }
        
        public void Set(float value)
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
