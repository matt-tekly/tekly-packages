using System.IO.Compression;
using Tekly.Balance;
using Tekly.Content;
using Tekly.Injectors;
using Tekly.Logging;
using Tekly.TreeState.StandardActivities;
using TeklySample.App;

namespace TeklySample.Game.Worlds
{
    public class GameWorldActivity : InjectableActivity
    {
        [Inject] private AppData m_appData;
        [Inject] private BalanceManager m_balanceManager;
        [Inject] private IContentProvider m_contentProvider;
        [Inject] private RootDataModel m_rootDataModel;
        [Inject] private InjectorContainer m_injectorContainer;

        private BalanceContainer m_balanceContainer;
        private bool m_isLoading;
        private readonly TkLogger m_logger = TkLogger.Get<GameWorldActivity>();
        private GameWorld m_gameWorld;
        private GameWorldModel m_gameWorldModel;
        
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
            m_rootDataModel.RemoveModel("gameworld");
            
            m_balanceManager.RemoveContainer(m_balanceContainer);
            m_balanceContainer.Dispose();
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
            m_gameWorld = new GameWorld(m_balanceManager, worldBalance);

            m_gameWorldModel = new GameWorldModel(m_gameWorld);
            m_rootDataModel.Add("gameworld", m_gameWorldModel);
            
            m_injectorContainer.Register(m_gameWorld);
        }

        protected override void ActiveUpdate()
        {
            m_gameWorld.Update();
        }
    }
}
