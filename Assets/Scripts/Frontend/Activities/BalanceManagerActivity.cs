using System;
using Tekly.Balance;
using Tekly.Injectors;
using Tekly.Logging;
using Tekly.TreeState.StandardActivities;

namespace TeklySample.Frontend.Activities
{
    public class BalanceManagerActivity : InjectableActivity
    {
        [Inject] private BalanceManager m_balanceManager;

        private bool m_loading;
        private readonly TkLogger m_logger = TkLogger.Get<BalanceManagerActivity>();
        
        protected override bool IsDoneLoading()
        {
            return !m_loading;
        }
        
        protected override void LoadingStarted()
        {
            LoadAsync();
        }

        private async void LoadAsync()
        {
            m_loading = true;
            
            try {
                await m_balanceManager.InitializeAsync();
                m_loading = false;
            } catch (Exception exception) {
                m_logger.Exception(exception, "Failed to initialize BalanceManager");
                HandleError();
            }
        }

        private void HandleError()
        {
            TreeState.HandleTransition("error");
        }
    }
}