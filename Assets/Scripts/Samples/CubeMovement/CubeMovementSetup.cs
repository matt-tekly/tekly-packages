using System;
using Tekly.Injectors;
using Tekly.Simulant.Core;
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

		public int TotalCubes => Rows * Columns * Layers;
	}

	/// <summary>
	/// Registers all the dependencies for the Cube Movement 
	/// </summary>
	public class CubeMovementSetup : MonoBehaviour, IInjectionProvider
	{
		[SerializeField] private CubeMovementConfig m_config;

		public void Provide(InjectorContainer container)
		{
			var world = new World(WorldConfig.Large);

			container.Register(world);
			container.Register(m_config);
			container.RegisterTypeProvider(new DataPoolProvider(world));

			container.Singleton<CubeSystem>();
			container.Singleton<TransformSystem>();
			container.Singleton<SystemsContainer>();
			container.Singleton<PrefabSystem>();
		}
	}
}