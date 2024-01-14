using Tekly.Injectors;
using Tekly.Simulant.Extensions.Systems;

namespace TeklySample.Samples.CubeMovement
{
	public class Systems
	{
		[Inject] private TransformSystem m_transformSystem;
		[Inject] private CubeSystem m_cubeSystem;

		public void Tick()
		{
			m_transformSystem.Tick();
			m_cubeSystem.Tick();
		}
		
		public void Dispose()
		{
			m_transformSystem.Dispose();
		}
	}
}