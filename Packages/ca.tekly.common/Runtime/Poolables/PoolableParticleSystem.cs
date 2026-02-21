using UnityEngine;

namespace Tekly.Common.Poolables
{
    /// <summary>
    /// A pooled particle system that will release itself when the particle system is done playing
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PoolableParticleSystem : Poolable
    {
        [SerializeField] private ParticleSystem m_particleSystem;
        [SerializeField] private bool m_releaseOnStop = true;

        private void Awake()
        {
            m_particleSystem = GetComponent<ParticleSystem>();

            var main = m_particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        private void OnParticleSystemStopped()
        {
            if (m_releaseOnStop) {
                Release();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_particleSystem == null) {
                m_particleSystem = GetComponent<ParticleSystem>();
            }
        }
#endif
    }
}