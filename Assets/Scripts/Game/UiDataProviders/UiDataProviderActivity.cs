using System.Collections.Generic;
using Tekly.Injectors;
using Tekly.TreeState.StandardActivities;

namespace TeklySample.Game.UiDataProviders
{
    /// <summary>
    /// Binds the list of given UiDataProviders. Each DataProvider is instantiated, injected into and then bound.
    /// When the Activity is unloaded the instances are unbound and cleared.
    /// </summary>
    public class UiDataProviderActivity : InjectableActivity
    {
        public List<UiDataProvider> DataProviders;

        [Inject] private InjectorContainer m_container;
        
        private List<UiDataProvider> m_instances = new List<UiDataProvider>();
        
        protected override void LoadingStarted()
        {
            if (m_instances.Count == 0) {
                foreach (var dataProvider in DataProviders) {
                    var instance = Instantiate(dataProvider);
                    m_instances.Add(instance);
                }
            }
            
            foreach (var instance in m_instances) {
                m_container.Inject(instance);
                instance.Bind();
            }
        }

        protected override void UnloadingStarted()
        {
            foreach (var instance in m_instances) {
                instance.Unbind();
                m_container.Clear(instance);
            }
        }
    }
}