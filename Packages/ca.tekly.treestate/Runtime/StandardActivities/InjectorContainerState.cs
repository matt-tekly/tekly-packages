using System;
using Tekly.Common.Utils;
using Tekly.Injectors;
using Tekly.Logging;

namespace Tekly.TreeState.StandardActivities
{
    public interface IInjectionProvider
    {
        void Provide(InjectorContainer container);
    }
    
    public class InjectorContainerState : TreeStateActivity
    {
        public string ParentRegistryId;
        public string SelfRegistryId;
        
        public InjectorContainer Container { get; private set; }
        public ScriptableBinding[] ScriptableBindings;
        
        private InjectorContainer m_parentContainer;
        
        private IInjectionProvider[] m_providers;

        private ScriptableBinding[] m_instances;
        
        private TkLogger m_logger = TkLogger.Get<InjectorContainerState>();
        
        protected override void Awake()
        {
            base.Awake();
            
            if (!string.IsNullOrEmpty(ParentRegistryId)) {
                if (!InjectorContainerRegistry.Instance.TryGet(ParentRegistryId, out m_parentContainer)) {
                    m_logger.ErrorContext("Failed to find InjectorContainer [{id}] in Registry", this, ("id", ParentRegistryId));
                }
            } else {
                var parent = transform.GetComponentInAncestor<InjectorContainerState>();
                if (parent != null) {
                    m_parentContainer = parent.Container;
                }
            }
            
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
            
            Container = new InjectorContainer(m_parentContainer);
            
            if (m_instances != null) {
                foreach (var scriptableInjector in m_instances) {
                    Container.Inject(scriptableInjector);
                    scriptableInjector.Bind(Container);
                }
            }
            
            foreach (var provider in m_providers) {
                provider.Provide(Container);
            }

            if (!string.IsNullOrEmpty(SelfRegistryId)) {
                InjectorContainerRegistry.Instance.Register(SelfRegistryId, Container);
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
            
            if (!string.IsNullOrEmpty(SelfRegistryId)) {
                InjectorContainerRegistry.Instance.Remove(SelfRegistryId);
            }
        }
    }
}