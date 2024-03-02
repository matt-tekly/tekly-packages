using Tekly.Simulant.Core;
using Tekly.Simulant.Systems;

namespace Tekly.Simulant.Extensions.Systems
{
	/// <summary>
	/// Deletes data T from all Entities
	/// </summary>
	public class DeleteDataSystem<T> : ITickEndSystem where T : struct
	{
		private readonly DataPool<T> m_dataPool;
		private readonly Query m_query;
		
		public DeleteDataSystem(World world)
		{
			m_dataPool = world.GetPool<T>();
			m_query = world.Query<T>();
		}
		
		public void TickEnd(float deltaTime)
		{
			foreach (var entity in m_query) {
				m_dataPool.Delete(entity);
			}
		}
	}
}