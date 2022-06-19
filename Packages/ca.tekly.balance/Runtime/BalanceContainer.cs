using System.Collections.Generic;
using System.Threading.Tasks;
using Tekly.Common.Utils;
using Tekly.Content;
using Tekly.Logging;

namespace Tekly.Balance
{
    public class BalanceContainer
    {
        public bool HasLoaded { get; private set; }

        public IList<BalanceObject> Objects { get; private set; }
        
        private IContentOperation<IList<BalanceObject>> m_handle;

        private readonly string m_balanceLabel;
        private readonly IContentProvider m_contentProvider;
        private readonly TkLogger m_logger = TkLogger.Get<BalanceContainer>();
        
        public BalanceContainer(string balanceLabel, IContentProvider contentProvider)
        {
            m_balanceLabel = balanceLabel;
            m_contentProvider = contentProvider;
        }

        public void Dispose()
        {
            m_handle.Release();
            m_handle = null;
        }

        public async Task<Result> LoadAsync()
        {
            m_logger.Debug("Loading balance started: [{balance}]", ("balance", m_balanceLabel));
            m_handle = m_contentProvider.LoadAssetsAsync<BalanceObject>(m_balanceLabel);
            var result = await m_handle;

            if (m_handle.HasError) {
                m_logger.Error("Failed to load Balance [{balance}]", ("balance", m_balanceLabel));
                Dispose();
                return Result.Fail("Failed to load Balance");
            }

            if (result == null || result.Count == 0) {
                m_logger.Error("Loaded Balance [{balance}] but found no objects", ("balance", m_balanceLabel));
                Dispose();
                return Result.Fail("Loaded Balance but found no objects");
            }

            m_logger.Debug("Loading balance finished: [{balance}]", ("balance", m_balanceLabel));
            
            Objects = result;
            HasLoaded = true;

            return Result.Okay();
        }
    }
}