using System.Threading.Tasks;
using Tekly.Content;
using Tekly.Injectors;
using Tekly.Logging;
using Tekly.TreeState.StandardActivities;

namespace TeklySample.Frontend.Activities
{
    public class ContentProviderActivity : AsyncInjectableActivity
    {
        [Inject] private IContentProvider m_contentProvider;
        
        private TkLogger m_logger = TkLogger.Get<ContentProviderActivity>();
        
        protected override async Task LoadAsync()
        {
            m_logger.Info("Initializing ContentProvider: Start");
            
            var result = await m_contentProvider.InitializeAsync();

            if (result.Failure) {
                m_logger.Error("Failed to initialize ContentProvider: [{error}]", ("error", result.Error));
                HandleError();
                return;
            }

            m_logger.Info("Initializing ContentProvider: Complete");
        }

        private void HandleError()
        {
            TreeState.HandleTransition("error");
        }
    }
}