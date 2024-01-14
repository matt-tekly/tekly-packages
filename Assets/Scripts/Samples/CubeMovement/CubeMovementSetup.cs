using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Injection;
using Tekly.Simulant.Extensions.Systems;
using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	public class CubeMovementSetup : MonoBehaviour, IInjectionProvider
	{
		[SerializeField] private CubeMovementConfig m_config;
		private World m_world;
		
		public void Provide(InjectorContainer container)
		{
			m_world = new World(WorldConfig.Large);
			
			container.Register(m_world);
			container.Register(m_config);
			container.RegisterTypeProvider(new DataPoolProvider(m_world));
			
			container.Singleton<CubeSystem>();
			container.Singleton<TransformSystem>();
			container.Singleton<Systems>();
		}
	}
}