using System;
using Tekly.Balance;
using Tekly.Common.LocalFiles;
using Tekly.Content;
using Tekly.Injectors;
using Tekly.Logging;
using Tekly.TreeState.StandardActivities;
using TeklySample.App;
using UnityEngine;

namespace TeklySample.Game.Worlds
{
    public class GameWorldActivity : InjectableActivity
    {
        [Inject] private AppData m_appData;
        [Inject] private BalanceManager m_balanceManager;
        [Inject] private IContentProvider m_contentProvider;
        [Inject] private InjectorContainer m_injectorContainer;

        private readonly TkLogger m_logger = TkLogger.Get<GameWorldActivity>();
        
        private bool m_isLoading;
        
        private GameWorld m_gameWorld;
        private GameWorldModel m_gameWorldModel;
        
        private BalanceContainer m_balanceContainer;
        
        protected override bool IsDoneLoading()
        {
            return !m_isLoading;
        }

        protected override void LoadingStarted()
        {
            m_logger.Info("GameWorld Loading Started: " + m_appData.ActiveWorld);
            m_balanceContainer = new BalanceContainer($"balance.{m_appData.ActiveWorld}", m_contentProvider);
            LoadAsync();
        }

        protected override void UnloadingStarted()
        {
            m_balanceManager.RemoveContainer(m_balanceContainer);
            m_balanceContainer.Dispose();

            Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void Save()
        {
            var saveData = m_gameWorld.ToSave();
            var saveJson = JsonUtility.ToJson(saveData);
            
            LocalFile.WriteAllText($"saves/{m_appData.ActiveWorld}.json", saveJson);
        }

        private async void LoadAsync()
        {
            m_isLoading = true;
            
            await m_balanceContainer.LoadAsync();
            m_balanceManager.AddContainer(m_balanceContainer);
            
            CreateGameWorld();
            
            m_isLoading = false;
            m_logger.Info("GameWorld Loading Finished: " + m_appData.ActiveWorld);
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
