using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;
using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	public struct CubeData
	{
		public Vector3 Origin;
	}
	
	public class CubeSystem
	{
		[Inject] private readonly World m_world;
		[Inject] private readonly CubeMovementConfig m_config;
		[Inject] private readonly DataPool<TransformData> m_transforms;
		[Inject] private readonly DataPool<CubeData> m_cubePool;
		
		private Query m_cubes;

		[Inject]
		private void Initialize()
		{
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

		private void GeneratePosition(in Vector3 origin, in float xRatio, in float zRatio, ref Vector3 position, float time)
		{
			position.x = origin.x;
			position.z = origin.z;
			position.y = origin.y * 1 + Mathf.PerlinNoise(xRatio + time, zRatio+ time) * 5;
		}
	}
}