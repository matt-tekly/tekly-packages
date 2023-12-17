using System;
using Tekly.Balance;
using Tekly.Common.TimeProviders;
using Tekly.Common.Utils;
using Tekly.Common.Utils.PropertyBags;
using Tekly.Content;
using Tekly.DataModels.Models;
using Tekly.Injectors;
using Tekly.Lofi.Core;
using Tekly.Logging;
using Tekly.TreeState;
using Tekly.Webster;
using UnityEngine;

namespace TeklySample.App
{
    public class AppCore : MonoBehaviour, IInjectionProvider
    {
        [SerializeField] private TimeProviderRef m_localTimeProviderRef;
        
        public void Provide(InjectorContainer container)
        {
            ITimeProvider localTimeProvider = new LocalTimeProvider();
            m_localTimeProviderRef.Initialize(localTimeProvider);

            var balanceManager = new BalanceManager(ContentProvider.Instance);

            container.Register(this);
            container.Register(new AppData());
            container.Register(ContentProvider.Instance);
            container.Register(balanceManager);
            container.Register(RootModel.Instance);
            container.Register(localTimeProvider);
            
            RootModel.Instance.Add("app", new AppModel(balanceManager));
            
            Lofi.Instance.SetPropertyBag(AppProperties.Instance);

            Debug.Log("Crash Detected: " + CrashCanary.Instance.CrashDetected);
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeDebug()
        {
            WebsterServer.Start(true);
            WebsterServer.AddRouteHandler<SampleWebsterHandler>();
            WebsterServer.AddRouteHandler<AppPropertiesRoute>();

            TreeStateRegistry.Instance.ActivityModeChanged.Subscribe(evt => {
                var type = evt.IsState ? "Tree State" : "Tree Activity";
                switch (evt.Mode) {
                    case ActivityMode.Inactive:
                        Frameline.EndEvent($"{evt.State} Unloading", type);
                        break;
                    case ActivityMode.Loading:
                        Frameline.BeginEvent($"{evt.State} Loading", type);
                        break;
                    case ActivityMode.ReadyToActivate:
                        Frameline.EndEvent($"{evt.State} Loading", type);
                        break;
                    case ActivityMode.Active:
                        Frameline.BeginEvent($"{evt.State} Active", type);
                        break;
                    case ActivityMode.Unloading:
                        Frameline.BeginEvent($"{evt.State} Unloading", type);
                        Frameline.EndEvent($"{evt.State} Active", type);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }
    }
}