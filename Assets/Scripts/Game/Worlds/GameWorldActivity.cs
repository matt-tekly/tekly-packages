using System;
using System.Threading.Tasks;
using Tekly.Balance;
using Tekly.Common.LocalFiles;
using Tekly.Common.Utils;
using Tekly.Content;
using Tekly.Injectors;
using Tekly.Lofi.Core;
using Tekly.Logging;
using Tekly.TreeState.StandardActivities;
using TeklySample.App;
using UnityEngine;

namespace TeklySample.Game.Worlds
{
    public class GameWorldActivity : AsyncInjectableActivity
    {
        [Inject] private AppData m_appData;
        [Inject] private BalanceManager m_balanceManager;
        [Inject] private IContentProvider m_contentProvider;
        [Inject] private InjectorContainer m_injectorContainer;

        private readonly TkLogger m_logger = TkLogger.Get<GameWorldActivity>();
        
        private GameWorld m_gameWorld;
        private GameWorldModel m_gameWorldModel;
        
        private IDisposable m_disposable;
        
        private void Save()
        {
            var saveData = m_gameWorld.ToSave();
            var saveJson = JsonUtility.ToJson(saveData);
            
            LocalFile.WriteAllText($"saves/{m_appData.ActiveWorld}.json", saveJson);
        }

        protected override async Task LoadAsync()
        {
            Lofi.Instance.LoadBank($"{m_appData.ActiveWorld}.clips");
            
            m_logger.Info("GameWorld Loading Started: " + m_appData.ActiveWorld);

            m_disposable = ApplicationFocusListener.Instance.Suspended.Subscribe(_ => Save());
            
            m_balanceManager.LoadBank($"balance.{m_appData.ActiveWorld}");

            while (Lofi.Instance.IsLoading || m_balanceManager.IsLoading) {
                await Task.Yield();
            }
            
            CreateGameWorld();
            
            m_logger.Info("GameWorld Loading Finished: " + m_appData.ActiveWorld);
        }
        
        protected override Task UnloadAsync()
        {
            m_logger.Info("Unloading");
            Lofi.Instance.UnloadBank($"{m_appData.ActiveWorld}.clips");
            m_disposable.Dispose();
            
            m_balanceManager.UnloadBank($"balance.{m_appData.ActiveWorld}");

            Save();
            
            return Task.CompletedTask;
        }

        private void CreateGameWorld()
        {
            var worldBalance = m_balanceManager.Get<WorldBalance>($"{m_appData.ActiveWorld}_world");

            GameWorldSave save = null;
            
            var saveFile = $"saves/{m_appData.ActiveWorld}.json";
            
            if (LocalFile.Exists(saveFile)) {
                var saveJson = LocalFile.ReadAllText(saveFile);
                save = JsonUtility.FromJson<GameWorldSave>(saveJson);
            }
            
            m_gameWorld = new GameWorld(m_balanceManager, worldBalance, save);

            m_injectorContainer.Register(m_gameWorld);
        }

        protected override void ActiveUpdate()
        {
            m_gameWorld.Update();
        }
    }
}
