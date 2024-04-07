using Tekly.Localizations;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace TeklySample.Frontend.Activities
{
    public class LoadLocalizationActivity : InjectableActivity
    {
        [SerializeField] private string m_key = "core_loc";
        
        protected override bool IsDoneLoading()
        {
            return Localizer.Instance.IsLoading;
        }
        
        protected override void LoadingStarted()
        {
            Localizer.Instance.LoadBank(m_key);
        }
    }
}