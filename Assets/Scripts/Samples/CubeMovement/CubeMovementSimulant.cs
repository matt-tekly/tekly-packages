using Tekly.Common.Collections;
using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;
using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	/// <summary>
	/// Creates all the Entities and sets up the Systems Container
	/// </summary>
	public class CubeMovementSimulant : MonoBehaviour
	{
		[SerializeField] private bool m_useGameObjects;
		[SerializeField] private PrefabData m_prefabData;
		[SerializeField] private BasicRendererData[] m_meshDatas;

		[Inject] private CubeMovementConfig m_config;
		[Inject] private World m_world;
		[Inject] private DataPool<TransformData> m_transforms;
		[Inject] private DataPool<GameObjectData> m_gameObjects;

		[Inject] private SystemsContainer m_systemsContainer;

		private void OnEnable()
		{
			gameObject.SetActive(true);

			for (var x = 0; x < m_config.Rows; x++) {
				for (var y = 0; y < m_config.Layers; y++) {
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
				.Add<MeshSystem>()
				.Init();
		}

		private void OnDisable()
		{
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
			var ratios = new Vector3(x / (float)m_config.Rows, y / (float)m_config.Layers, z / (float)m_config.Columns);

			CreateEntity(position, ratios);
		}

		private void CreateEntity(Vector3 position, Vector3 ratios)
		{
			var entity = m_world.Create();

			if (m_useGameObjects) {
				m_world.Add(entity, m_prefabData);
			} else {
				m_world.Add(entity, m_meshDatas.Random());
			}

			ref var transformData = ref m_world.Add<TransformData>(entity);
			transformData.Position = position;
			transformData.Scale = Vector3.one;

			ref var cubeData = ref m_world.Add<CubeData>(entity);
			cubeData.Origin = position;
			cubeData.Ratios = ratios;
		}
	}
}