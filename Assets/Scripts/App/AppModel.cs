using Tekly.Balance;
using Tekly.DataModels.Models;
using UnityEngine;

namespace TeklySample.App
{
    public class AppModel : ObjectModel
    {
        public readonly AppBalanceModel AppBalance;
        private readonly StringValueModel m_version = new(Application.version);

        public AppModel(BalanceManager balanceManager)
        {
            AppBalance = new AppBalanceModel(balanceManager);
            Add("balance", AppBalance);
            Add("version", m_version);
        }
    }

    public class AppBalanceModel : ObjectModel
    {
        private readonly BalanceManager m_balanceManager;
        private readonly StringValueModel m_version = new("x.x.x");
        private readonly BoolValueModel m_initialized = new(false);
        
        public AppBalanceModel(BalanceManager balanceManager)
        {
            m_balanceManager = balanceManager;
            Add("version", m_version);
            Add("initialized", m_initialized);
        }
        
        protected override void OnTick()
        {
            m_version.AsString = m_balanceManager.Version;
            m_initialized.AsBool = m_balanceManager.IsInitialized;
        }
    }
}