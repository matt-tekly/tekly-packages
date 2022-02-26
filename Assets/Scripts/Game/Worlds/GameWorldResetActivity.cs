using Tekly.Common.LocalFiles;
using Tekly.Injectors;
using Tekly.TreeState.StandardActivities;
using TeklySample.App;

namespace TeklySample.Game.Worlds
{
    public class GameWorldResetActivity : InjectableActivity
    {
        [Inject] private AppData m_appData;
        
        protected override void LoadingStarted()
        {
            LocalFile.Delete($"saves/{m_appData.ActiveWorld}.json");
        }
    }
}