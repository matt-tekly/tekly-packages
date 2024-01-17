using System;
using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Debug;
using Tekly.Simulant.Extensions.Injection;
using Tekly.Simulant.Extensions.Systems;
using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	[Serializable]
	public class CubeMovementConfig
	{
		public float Size = 5;
		public int Rows = 100;
		public int Columns = 100;
		public int Layers = 2;
	}

	/// <summary>
	/// Registers all the dependencies for the Cube Movement 
	/// </summary>
	public class CubeMovementSetup : MonoBehaviour, IInjectionProvider
	{
		[SerializeField] private CubeMovementConfig m_config;
		[SerializeField] private Camera m_camera;

		public void Provide(InjectorContainer container)
		{
			var world = new World(WorldConfig.Large);
			DebugWorlds.Instance.Add(world, "Cubes");
			
			container.Register(world);
			container.Register(m_config);
			container.Register(m_camera);
			
			container.RegisterTypeProvider(new DataPoolProvider(world));

			container.Singleton<CubeSystem>();
			container.Singleton<TransformSystem>();
			container.Singleton<SystemsContainer>();
			container.Singleton<PrefabSystem>();
			container.Singleton<MeshSystem>();
		}
	}
}