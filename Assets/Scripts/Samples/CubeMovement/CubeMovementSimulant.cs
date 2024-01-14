using System.Collections.Generic;
using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;
using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	public class CubeMovementSimulant : MonoBehaviour
	{
		[SerializeField] private GameObject m_template;

		[Inject] private CubeMovementConfig m_config;
		[Inject] private World m_world;
		[Inject] private DataPool<TransformData> m_transforms;
		[Inject] private DataPool<GameObjectData> m_gameObjects;

		[Inject] private SystemsContainer m_systemsContainer;

		private List<GameObject> m_instances = new List<GameObject>();

		private void OnEnable()
		{
			gameObject.SetActive(true);

			m_instances.Clear();
			m_instances.Capacity = m_config.TotalCubes;

			for (var x = 0; x < m_config.Rows; x++) {
				for (var y = 0; y < m_config.Planes; y++) {
					for (var z = 0; z < m_config.Columns; z++) {
						CreateEntity(x, y, z);
					}
				}
			}

			// The SystemsContainer will use the InjectorContainer to get instances of the Systems you register
			// They will Tick in the order they are added.
			m_systemsContainer
				.Add<PrefabSystem>()
				.Add<CubeSystem>()
				.Add<TransformSystem>()
				.Init();
		}

		private void OnDisable()
		{
			foreach (var instance in m_instances) {
				Destroy(instance);
			}

			m_world.Destroy();
			m_systemsContainer.Dispose();
		}

		private void OnDestroy()
		{
			m_systemsContainer.Dispose();
		}

		private void Update()
		{
			m_systemsContainer?.Tick(Time.deltaTime);
		}

		private void CreateEntity(int x, int y, int z)
		{
			var position = new Vector3(x, y * 5f, z) * m_config.Size;
			var ratios = new Vector3(x / (float)m_config.Rows, y / (float)m_config.Planes, z / (float)m_config.Columns);

			CreateEntity(position, ratios);
		}

		private void CreateEntity(Vector3 position, Vector3 ratios)
		{
			var entity = m_world.Create();
			
			m_world.Add(entity, new PrefabData {
				Prefab = m_template 
			});

			ref var transformData = ref m_world.Add<TransformData>(entity);
			transformData.Position = position;
			
			ref var cubeData = ref m_world.Add<CubeData>(entity);
			cubeData.Origin = position;
			cubeData.Ratios = ratios;
		}
	}
}