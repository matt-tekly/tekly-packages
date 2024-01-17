namespace Tekly.Simulant.Core
{
	public class Query<T1> where T1: struct
	{
		public delegate void ActionEntityRef(int entity, ref T1 item1);
		public delegate void ActionRef(ref T1 item1);
		
		private readonly Query m_query;
		private readonly DataPool<T1> m_pool;
		
		public Query.Enumerator GetEnumerator()
		{
			return m_query.GetEnumerator();
		}
		
		public Query(World world)
		{
			m_query = world.Query<T1>();
			m_pool = world.GetPool<T1>();
		}
		
		public void ForEach(ActionEntityRef action)
		{
			foreach (var entity in m_query) {
				ref var data = ref m_pool.Get(entity);
				action(entity, ref data);
			}
		}
		
		public void ForEach(ActionRef action)
		{
			foreach (var entity in m_query) {
				ref var data = ref m_pool.Get(entity);
				action(ref data);
			}
		}
	}
	
	public class Query<T1, T2> where T1: struct where T2: struct
	{
		public delegate void ActionEntityRef(int entity, ref T1 item1, ref T2 item2);
		public delegate void ActionRef(ref T1 item1, ref T2 item2);
		
		private readonly Query m_query;
		private readonly DataPool<T1> m_pool1;
		private readonly DataPool<T2> m_pool2;
		
		public Query(World world)
		{
			m_query = world.Query<T1>();
			m_pool1 = world.GetPool<T1>();
			m_pool2 = world.GetPool<T2>();
		}

		public Query.Enumerator GetEnumerator()
		{
			return m_query.GetEnumerator();
		}
		
		public void ForEach(ActionEntityRef action)
		{
			foreach (var entity in m_query) {
				ref var data1 = ref m_pool1.Get(entity);
				ref var data2 = ref m_pool2.Get(entity);
				action(entity, ref data1, ref data2);
			}
		}
		
		public void ForEach(ActionRef action)
		{
			foreach (var entity in m_query) {
				ref var data1 = ref m_pool1.Get(entity);
				ref var data2 = ref m_pool2.Get(entity);
				action(ref data1, ref data2);
			}
		}
	}
}