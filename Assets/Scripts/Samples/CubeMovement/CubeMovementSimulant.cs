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
		
		[Inject] private Systems m_systems;
		
		private List<GameObject> m_instances = new List<GameObject>();

		private void OnEnable()
		{
			gameObject.SetActive(true);

			m_instances.Clear();
			m_instances.Capacity = m_config.TotalCubes;
			
			for (var x = 0; x < m_config.Rows; x++) {
				for (var y = 0; y < m_config.Planes; y++) {
					for (var z = 0; z < m_config.Columns; z++) {
						var position = new Vector3(x, y * 5f, z) * m_config.Size;
						CreateEntity(position);
					}
				}
			}
		}

		private void OnDisable()
		{
			gameObject.SetActive(false);
			foreach (var instance in m_instances) {
				Destroy(instance);
			}

			m_systems.Dispose();
		}

		private void OnDestroy()
		{
			m_systems.Dispose();
		}

		private void Update()
		{
			if (m_systems != null) {
				m_systems.Tick();	
			}
		}

		private void CreateEntity(Vector3 position)
		{
			var instance = Instantiate(m_template, position, Quaternion.identity);
			m_instances.Add(instance);
			var entity = m_world.Create();

			ref var transformData = ref m_world.Add<TransformData>(entity);
			transformData.Position = position;

			ref var gameObjectData = ref m_world.Add<GameObjectData>(entity);
			gameObjectData.GameObject = instance;
			gameObjectData.Transform = instance.transform;
			
			ref var cubeData = ref m_world.Add<CubeData>(entity);
			cubeData.Origin = position;
		}
	}
}