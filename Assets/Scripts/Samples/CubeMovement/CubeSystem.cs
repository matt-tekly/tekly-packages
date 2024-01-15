using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;
using TMPro;
using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	public struct CubeData
	{
		public Vector3 Origin;
		public Vector3 Ratios;
	}

	/// <summary>
	/// Moves the cubes TransformData around
	/// </summary>
	public class CubeSystem : ITickSystem
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

		public void Tick(float deltaTime)
		{
			var time = Time.time;

			var position = Vector3.zero;
			var up = Vector3.up;

			foreach (var cube in m_cubes) {
				ref var transformData = ref m_transforms.Get(cube);
				ref var cubeData = ref m_cubePool.Get(cube);

				GeneratePosition(in cubeData.Origin, in cubeData.Ratios, ref position, time);
				transformData.Position = position;
				transformData.Rotation = Quaternion.AngleAxis(Mathf.Sin(time + cubeData.Ratios.x) * 360, up);
			}
		}

		private void GeneratePosition(in Vector3 origin, in Vector3 ratios, ref Vector3 position, float time)
		{
			position.x = origin.x;
			position.z = origin.z;
			position.y = origin.y * 1 + Mathf.PerlinNoise(ratios.x + time, ratios.y + time) * 5;
		}
	}
}