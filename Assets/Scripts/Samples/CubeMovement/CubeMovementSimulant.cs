using System.Collections.Generic;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	public struct CubeData
	{
		public Vector3 Origin;
	}
	
	public class CubeSystem
	{
		private readonly World m_world;
		private readonly CubeMovementConfig m_config;
		private readonly Query m_cubes;
		
		private readonly DataPool<TransformData> m_transforms;
		private readonly DataPool<CubeData> m_cubePool;
		
		public CubeSystem(World world, CubeMovementConfig config)
		{
			m_world = world;
			m_config = config;

			m_transforms = m_world.GetPool<TransformData>();
			m_cubePool = m_world.GetPool<CubeData>();
			
			m_cubes = m_world.Query().Include<TransformData, CubeData>().Build();
		}

		public void Tick()
		{
			var time = Time.time;
			
			Vector3 position = default;
			foreach (var cube in m_cubes) {
				ref var transformData = ref m_transforms.Get(cube);
				ref var cubeData = ref m_cubePool.Get(cube);

				var xRatio = cubeData.Origin.x / m_config.Rows;
				var zRatio = cubeData.Origin.z / m_config.Columns;
				
				GeneratePosition(in cubeData.Origin, in xRatio, in zRatio, ref position, time);
				transformData.Position = position;
				transformData.Rotation = Quaternion.AngleAxis(Mathf.Sin(time + xRatio) * 360, new Vector3(0, 1, 0));
			}
		}
		
		public void GeneratePosition(in Vector3 origin, in float xRatio, in float zRatio, ref Vector3 position, float time)
		{
			position.x = origin.x;
			position.z = origin.z;
			position.y = origin.y * 1 + Mathf.PerlinNoise(xRatio + time, zRatio+ time) * 5;
		}
	}
	
	public class CubeMovementSimulant : MonoBehaviour
	{
		[SerializeField] private GameObject m_template;
		[SerializeField] private CubeMovementConfig m_config;

		private World m_world;
		private Query m_cubes;

		private DataPool<TransformData> m_transforms;
		private DataPool<GameObjectInstance> m_gameObjects;

		private TransformSystem m_transformSystem;
		private CubeSystem m_cubeSystem;

		private List<GameObject> m_instances = new List<GameObject>();

		private void OnEnable()
		{
			gameObject.SetActive(true);

			m_instances.Clear();
			m_instances.Capacity = m_config.TotalCubes;
			
			m_world = new World(WorldConfig.Large);

			for (var x = 0; x < m_config.Rows; x++) {
				for (var y = 0; y < m_config.Planes; y++) {
					for (var z = 0; z < m_config.Columns; z++) {
						var position = new Vector3(x, y * 5f, z) * m_config.Size;
						CreateEntity(position);
					}
				}
			}

			m_transformSystem = new TransformSystem(m_world);
			m_cubeSystem = new CubeSystem(m_world, m_config);
		}

		private void OnDisable()
		{
			gameObject.SetActive(false);
			foreach (var instance in m_instances) {
				Destroy(instance);
			}

			m_transformSystem.Dispose();
		}

		private void OnDestroy()
		{
			m_transformSystem.Dispose();
		}

		private void Update()
		{
			m_transformSystem.Tick();
			m_cubeSystem.Tick();
		}

		private void CreateEntity(Vector3 position)
		{
			var instance = Instantiate(m_template, position, Quaternion.identity);
			m_instances.Add(instance);
			var entity = m_world.Create();

			ref var transformData = ref m_world.Add<TransformData>(entity);
			transformData.Position = position;

			ref var gameObjectData = ref m_world.Add<GameObjectInstance>(entity);
			gameObjectData.Transform = instance.transform;
			
			ref var cubeData = ref m_world.Add<CubeData>(entity);
			cubeData.Origin = position;
		}
	}
}