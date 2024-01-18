using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;
using Tekly.Simulant.Systems;
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

		public void Tick(float deltaTime)
		{
			var time = Time.time;

			var position = Vector3.zero;
			var axis = new Vector3(0.25f, 0.25f, 0.25f);
			
			m_world.ForEach((ref CubeData cubeData, ref TransformData transformData) => {
				GeneratePosition(in cubeData.Origin, in cubeData.Ratios, ref position, time);
				transformData.Position = position;
				transformData.Rotation = Quaternion.AngleAxis(Mathf.Sin(time + cubeData.Ratios.x) * 360, axis);
			
				var scale = 1 + Mathf.PerlinNoise(cubeData.Ratios.x + time, cubeData.Ratios.y + time);
				transformData.Scale = new Vector3(scale, scale, scale);
			});
		}

		private void GeneratePosition(in Vector3 origin, in Vector3 ratios, ref Vector3 position, float time)
		{
			position.x = origin.x;
			position.z = origin.z;
			position.y = origin.y + Mathf.PerlinNoise(ratios.x + time, ratios.y + time) * 5;
		}
	}
}