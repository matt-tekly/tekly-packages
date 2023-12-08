using Tekly.Lofi.Core;
using Tekly.TreeState.StandardActivities;

namespace TeklySample.App
{
	public class BootActivity : InjectableActivity
	{
		protected override bool IsDoneLoading()
		{
			return !Lofi.Instance.IsLoading;
		}

		protected override void LoadingStarted()
		{
			Lofi.Instance.LoadBank("common.clips");
		}
	}
}