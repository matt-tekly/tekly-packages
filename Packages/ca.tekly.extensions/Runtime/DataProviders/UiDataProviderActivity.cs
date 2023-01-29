using Tekly.Common.Utils;
using Tekly.Injectors;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace Tekly.Extensions.DataProviders
{
    public interface IUiDataProvider
    {
        void Bind();
        void Unbind();
    }

    /// <summary>
    /// Binds the list of given UiDataProviders. Each DataProvider is injected into and then bound.
    /// When the Activity is unloaded the instances are unbound and cleared.
    /// </summary>
    public class UiDataProviderActivity : InjectableActivity
    {
        [SerializeReference, Polymorphic("Data Providers")] private IUiDataProvider[] m_providers;
        [Inject] private InjectorContainer m_container;
        
        protected override void LoadingStarted()
        {
            foreach (var provider in m_providers) {
                m_container.Inject(provider);
                provider.Bind();
            }
        }

        protected override void UnloadingStarted()
        {
            foreach (var provider in m_providers) {
                provider.Unbind();
                m_container.Clear(provider);
            }
        }
    }
}