using System.Threading.Tasks;
using Tekly.Balance;
using Tekly.Injectors;
using Tekly.Logging;
using Tekly.TreeState.StandardActivities;

namespace TeklySample.Frontend.Activities
{
    public class BalanceManagerActivity : AsyncInjectableActivity
    {
        [Inject] private BalanceManager m_balanceManager;
        
        private TkLogger m_logger = TkLogger.Get<BalanceManagerActivity>();
        
        protected override async Task LoadAsync()
        {
            var result = await m_balanceManager.InitializeAsync();

            if (!result.Success) {
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