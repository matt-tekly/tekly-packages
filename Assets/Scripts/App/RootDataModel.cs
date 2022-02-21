using Tekly.Balance;
using Tekly.DataModels.Models;

namespace TeklySample.App
{
    public class RootDataModel : ObjectModel
    {
        public readonly AppModel App;
        
        public RootDataModel(BalanceManager balanceManager)
        {
            App = new AppModel(balanceManager);
            Add("app", App);
            Instance = this;
        }
    }
}