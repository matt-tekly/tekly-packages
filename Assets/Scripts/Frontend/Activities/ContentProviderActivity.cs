using Tekly.Content;
using Tekly.Injectors;
using Tekly.Logging;
using Tekly.TreeState.StandardActivities;

namespace TeklySample.Frontend.Activities
{
    public class ContentProviderActivity : InjectableActivity
    {
        [Inject] private IContentProvider m_contentProvider;

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
            m_logger.Info("Initializing ContentProvider: Start");
            
            var result = await m_contentProvider.InitializeAsync();

            if (result.Failure) {
                m_logger.Error("Failed to initialize ContentProvider: [{error}]", ("error", "Failed to initialize ContentProvider"));
                HandleError();
                return;
            }

            m_loading = false;
            m_logger.Info("Initializing ContentProvider: Complete");
        }

        private void HandleError()
        {
            TreeState.HandleTransition("error");
        }
    }
}