using System.Threading.Tasks;

namespace Tekly.TreeState.StandardActivities
{
	public abstract class AsyncInjectableActivity : InjectableActivity
	{
		private Task m_loadTask;
		private Task m_unloadTask;
        
		protected override bool IsDoneLoading()
		{
			if (m_loadTask == null) {
				return false;
			}

			return m_loadTask.IsCompleted;
		}
		
		protected override bool IsDoneUnloading()
		{
			if (m_unloadTask == null) {
				return false;
			}

			return m_unloadTask.IsCompleted;
		}
        
		protected sealed override void LoadingStarted()
		{
			m_loadTask = LoadAsync();
		}
		
		protected sealed override void UnloadingStarted()
		{
			m_unloadTask = UnloadAsync();
		}

		protected virtual Task LoadAsync()
		{
			return Task.CompletedTask;
		}
		
		protected virtual Task UnloadAsync()
		{
			return Task.CompletedTask;
		}

		protected override void PostInactive()
		{
			m_loadTask = null;
			m_unloadTask = null;
		}
	}
}