using System.Threading.Tasks;
using UnityEngine;

namespace Tekly.TreeState.StandardActivities
{
	public abstract class AsyncInjectableActivity : InjectableActivity
	{
		private Task m_loadTask;
		private Task m_unloadTask;
        
		protected sealed override bool IsDoneLoading()
		{
			if (m_loadTask == null) {
				return false;
			}

			if (m_loadTask.IsFaulted) {
				Debug.LogException(m_loadTask.Exception);
			}

			return m_loadTask.IsCompleted;
		}
		
		protected sealed override bool IsDoneUnloading()
		{
			if (m_unloadTask == null) {
				return false;
			}
			
			if (m_unloadTask.IsFaulted) {
				Debug.LogException(m_unloadTask.Exception);
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