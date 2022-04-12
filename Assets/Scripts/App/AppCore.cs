using Tekly.Balance;
using Tekly.Common.Terminal.Commands;
using Tekly.Content;
using Tekly.DataModels.Models;
using Tekly.Glass;
using Tekly.Injectors;
using Tekly.Logging;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace TeklySample.App
{
    public class AppCore : MonoBehaviour, IInjectionProvider
    {
        public Glass Glass;

        public void Provide(InjectorContainer container)
        {
            var balanceManager = new BalanceManager(ContentProvider.Instance);

            container.Register(this);
            container.Register(new AppData());
            container.Register(ContentProvider.Instance);
            container.Register(balanceManager);
            container.Register(RootModel.Instance);
            
            container.Register(Glass);
            
            RootModel.Instance.Add("app", new AppModel(balanceManager));
            
            CommandStore.Instance.AddCommandSource(new GameCommands());
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                Application.Quit();
                return;
            }

            ModelManager.Instance.Tick();
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            TkLogger.Initialize();
        }
    }
}
