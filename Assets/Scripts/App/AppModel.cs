using Tekly.Balance;
using Tekly.DataModels.Models;
using UnityEngine;

namespace TeklySample.App
{
    public class AppModel : ObjectModel
    {
        private readonly StringValueModel m_version = new StringValueModel(Application.version);

        public AppModel(BalanceManager balanceManager)
        {
            Add("balance", new AppBalanceModel(balanceManager));
            Add("version", m_version);
        }
    }

    public class AppBalanceModel : ObjectModel, ITickable
    {
        private readonly BalanceManager m_balanceManager;
        private readonly StringValueModel m_version = new StringValueModel("x.x.x");
        private readonly BoolValueModel m_initialized = new BoolValueModel(false);
        
        public AppBalanceModel(BalanceManager balanceManager)
        {
            m_balanceManager = balanceManager;
            Add("version", m_version);
            Add("initialized", m_initialized);
        }
        
        protected override void OnTick()
        {
            m_version.Value = m_balanceManager.Version;
            m_initialized.Value = m_balanceManager.IsInitialized;
        }
    }
}