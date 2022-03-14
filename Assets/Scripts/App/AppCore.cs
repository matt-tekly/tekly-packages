using Tekly.Balance;
using Tekly.Content;
using Tekly.DataModels.Models;
using Tekly.Glass;
using Tekly.Injectors;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace TeklySample.App
{
    public class AppCore : MonoBehaviour, IInjectionProvider
    {
        public Glass Glass;
        
        private RootDataModel m_rootDataModel;
        
        public void Provide(InjectorContainer container)
        {
            var balanceManager = new BalanceManager(ContentProvider.Instance);

            container.Register(this);
            container.Register(new AppData());
            container.Register(ContentProvider.Instance);
            container.Register(balanceManager);

            m_rootDataModel = new RootDataModel(balanceManager);
            container.Register(m_rootDataModel);
            
            container.Register(Glass);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                Application.Quit();
                return;
            }

            ModelManager.Instance.Tick();
        }
    }
}
