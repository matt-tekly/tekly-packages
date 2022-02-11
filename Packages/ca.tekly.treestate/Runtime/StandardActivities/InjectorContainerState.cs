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
        
        private InjectorContainerState m_parent;
        private IInjectionProvider[] m_providers;
        
        protected override void Awake()
        {
            base.Awake();
            m_parent = transform.GetComponentInAncestor<InjectorContainerState>();
            m_providers = GetComponents<IInjectionProvider>();
        }
        
        protected override void PreLoad()
        {
            if (m_parent != null) {
                Container = new InjectorContainer(m_parent.Container);
            } else {
                Container = new InjectorContainer();
            }
            
            foreach (var provider in m_providers) {
                provider.Provide(Container);
            }
        }

        protected override void InactiveStarted()
        {
            Container = null;
        }
    }
}