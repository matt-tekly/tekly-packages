namespace Tekly.Simulant.Core
{
	public partial class World
	{
		public Query Query<T>() where T : struct
		{
			return Query().Include<T>().Build();
		}

		public Query Query<T1, T2>() where T1 : struct where T2 : struct
		{
			return Query().Include<T1, T2>().Build();
		}
		
		public void ForEach<T1>(Query<T1>.ActionRef action) where T1 : struct
		{
			var query = Query<T1>();
			var pool1 = GetPool<T1>();
			
			foreach (var entity in query) {
				ref var data1 = ref pool1.Get(entity);
				action(ref data1);
			}
		}
		
		public void ForEach<T1>(Query<T1>.ActionEntityRef action) where T1 : struct
		{
			var query = Query<T1>();
			var pool1 = GetPool<T1>();
			
			foreach (var entity in query) {
				ref var data1 = ref pool1.Get(entity);
				action(entity, ref data1);
			}
		}
		
		public void ForEach<T1, T2>(Query<T1, T2>.ActionRef action) where T1 : struct where T2 : struct
		{
			var query = Query<T1, T2>();
			var pool1 = GetPool<T1>();
			var pool2 = GetPool<T2>();
			
			foreach (var entity in query) {
				ref var data1 = ref pool1.Get(entity);
				ref var data2 = ref pool2.Get(entity);
				action(ref data1, ref data2);
			}
		}
		
		public void ForEach<T1, T2>(Query<T1, T2>.ActionEntityRef action) where T1 : struct where T2 : struct
		{
			var query = Query<T1, T2>();
			var pool1 = GetPool<T1>();
			var pool2 = GetPool<T2>();
			
			foreach (var entity in query) {
				ref var data1 = ref pool1.Get(entity);
				ref var data2 = ref pool2.Get(entity);
				action(entity, ref data1, ref data2);
			}
		}
	}
}