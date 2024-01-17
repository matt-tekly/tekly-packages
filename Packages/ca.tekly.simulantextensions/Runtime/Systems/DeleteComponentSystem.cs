using Tekly.Simulant.Core;

namespace Tekly.Simulant.Extensions.Systems
{
	/// <summary>
	/// Deletes component T from all Entities
	/// </summary>
	public class DeleteComponentSystem<T> : ITickSystem where T : struct
	{
		private readonly DataPool<T> m_dataPool;
		private readonly Query m_query;
		
		public DeleteComponentSystem(World world)
		{
			m_dataPool = world.GetPool<T>();
			m_query = world.Query<T>();
		}
		
		public void Tick(float deltaTime)
		{
			foreach (var entity in m_query) {
				m_dataPool.Delete(entity);
			}
		}
	}
}