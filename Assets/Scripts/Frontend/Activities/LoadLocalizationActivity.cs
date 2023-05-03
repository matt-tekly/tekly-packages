using Tekly.Content;
using Tekly.Injectors;
using Tekly.Localizations;
using Tekly.Logging;
using Tekly.TreeState.StandardActivities;
using Tekly.Webster;
using Tekly.Webster.FramelineCore;

namespace TeklySample.Frontend.Activities
{
    public class LoadLocalizationActivity : InjectableActivity
    {
        [Inject] private IContentProvider m_contentProvider;
        private readonly TkLogger m_logger = TkLogger.Get<BalanceManagerActivity>();
        private IContentOperation<LocalizationData> m_handle;

        private bool m_doneLoading;
        
        protected override bool IsDoneLoading()
        {
            return m_doneLoading;
        }
        
        protected override void LoadingStarted()
        {
            m_handle = m_contentProvider.LoadAssetAsync<LocalizationData>("en_US_loc");
        }

        protected override void LoadingUpdate()
        {
            if (!m_handle.IsDone) {
                return;
            }

            if (m_handle.HasError) {
                m_logger.Error("Failed to load LocalizationData");
            }

            Localizer.Instance.AddData(m_handle.Result);
            
            m_handle.Release();

            m_doneLoading = true;
        }
    }
}