using System;
using Tekly.Common.Utils;
using Tekly.Injectors;

namespace Tekly.TreeState.StandardActivities
{
    public interface IInjectionProvider
    {
        void Provide(InjectorContainer container);
    }
    
    public class InjectorContainerState : TreeStateActivity
    {
        public InjectorContainer Container;
        public ScriptableBinding[] ScriptableBindings;
        
        private InjectorContainerState m_parent;
        private IInjectionProvider[] m_providers;

        private ScriptableBinding[] m_instances;
        
        protected override void Awake()
        {
            base.Awake();
            m_parent = transform.GetComponentInAncestor<InjectorContainerState>();
            m_providers = GetComponents<IInjectionProvider>();
        }
        
        protected override void PreLoad()
        {
            if (ScriptableBindings != null && ScriptableBindings.Length > 0 && m_instances == null) {
                Array.Resize(ref m_instances, ScriptableBindings.Length);
                for (var index = 0; index < ScriptableBindings.Length; index++) {
                    var scriptableInjector = ScriptableBindings[index];
                    m_instances[index] = Instantiate(scriptableInjector);
                }
            }
            
            if (m_parent != null) {
                Container = new InjectorContainer(m_parent.Container);
            } else {
                Container = new InjectorContainer();
            }

            if (m_instances != null) {
                foreach (var scriptableInjector in m_instances) {
                    Container.Inject(scriptableInjector);
                    scriptableInjector.Bind(Container);
                }
            }
            
            foreach (var provider in m_providers) {
                provider.Provide(Container);
            }
        }

        protected override void InactiveStarted()
        {
            if (m_instances != null) {
                foreach (var scriptableInjector in m_instances) {
                    Container.Clear(scriptableInjector);
                }
            }
            
            Container = null;   
        }
    }
}