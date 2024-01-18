using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;
using Tekly.Simulant.Systems;
using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	public class CubeSystemCached : ITickSystem
	{
		[Inject] private readonly World m_world;
		[Inject] private readonly DataPool<CubeData> m_cubeData;
		[Inject] private readonly DataPool<TransformData> m_transformData;

		private Query m_cubes;

		[Inject]
		private void Initialize()
		{
			m_cubes = m_world.Query<CubeData, TransformData>();
		}

		public void Tick(float deltaTime)
		{
			var time = Time.time;

			var position = Vector3.zero;
			var axis = new Vector3(0.25f, 0.25f, 0.25f);
			
			foreach (var entity in m_cubes) {
				ref var cubeData = ref m_cubeData.Get(entity);
				ref var transformData = ref m_transformData.Get(entity);
				GeneratePosition(in cubeData.Origin, in cubeData.Ratios, ref position, time);
				transformData.Position = position;
				transformData.Rotation = Quaternion.AngleAxis(Mathf.Sin(time + cubeData.Ratios.x) * 360, axis);

				var scale = 1 + Mathf.PerlinNoise(cubeData.Ratios.x + time, cubeData.Ratios.y + time);
				transformData.Scale = new Vector3(scale, scale, scale);
			}
		}

		private void GeneratePosition(in Vector3 origin, in Vector3 ratios, ref Vector3 position, float time)
		{
			position.x = origin.x;
			position.z = origin.z;
			position.y = origin.y + Mathf.PerlinNoise(ratios.x + time, ratios.y + time) * 5;
		}
	}
}