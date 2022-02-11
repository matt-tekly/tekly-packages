using Tekly.Injectors;

namespace Tekly.TreeState.StandardActivities
{
    public class InjectableActivity : TreeStateActivity
    {
        private InjectorContainerState m_injectorContainerState;
        private InjectorContainer m_container;
        
        protected override void Awake()
        {
            base.Awake();
            m_injectorContainerState = GetComponentInParent<InjectorContainerState>();
        }
        
        protected override void PreLoad()
        {
            m_container = m_injectorContainerState.Container; 
            m_container.Inject(this);
            base.PreLoad();
        }

        protected override void PostInactive()
        {
            base.PostInactive();
            m_container.Clear(this);
        }
    }
}