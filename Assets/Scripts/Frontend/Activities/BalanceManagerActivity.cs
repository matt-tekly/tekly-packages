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

            var result = await m_balanceManager.InitializeAsync();

            if (result.Success) {
                m_loading = false;
            } else {
                m_logger.Error(result.Error);
                HandleError();
            }
        }

        private void HandleError()
        {
            TreeState.HandleTransition("error");
        }
    }
}