using System;

namespace Tekly.Simulant.Core
{
	public delegate void EntityDelegate(int entity);
	
	internal class QueryDelegateListener : IQueryListener, IDisposable
	{
		private readonly EntityDelegate m_add;
		private readonly EntityDelegate m_remove;
		private readonly Query m_query;

		public QueryDelegateListener(EntityDelegate add, EntityDelegate remove, Query query)
		{
			m_add = add;
			m_remove = remove;
			m_query = query;
		}

		public void Dispose() => m_query.RemoveListener(this);

		public void EntityAdded(int entity, Query query)
		{
			m_add?.Invoke(entity);	
		}

		public void EntityRemoved(int entity, Query query)
		{
			m_remove?.Invoke(entity);
		}
	}

	public partial class Query
	{
		public IDisposable Listen(EntityDelegate add, EntityDelegate remove)
		{
			var listener = new QueryDelegateListener(add, remove, this);
			m_listeners.Add(listener);

			return listener;
		}
		
		public IDisposable ListenAdd(EntityDelegate add)
		{
			return Listen(add, null);
		}
		
		public IDisposable ListenRemove(EntityDelegate remove)
		{
			return Listen(null, remove);
		}
	}
}