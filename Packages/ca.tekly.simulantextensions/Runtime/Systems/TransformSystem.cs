using System;
using Tekly.Simulant.Core;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace Tekly.Simulant.Extensions.Systems
{
	public struct TransformData
	{
		public Vector3 Position;
		public Quaternion Rotation;
		public Vector3 Scale;
	}

	public struct GameObjectData : ITransient
	{
		public GameObject GameObject;
		public Transform Transform;
	}
	
	public class TransformSystem : ITickSystem, IDisposable
	{
		[BurstCompile(CompileSynchronously = true)]
		private struct TransformSystemJob : IJobParallelForTransform
		{
			[ReadOnly] public NativeArray<TransformData> Data;

			public void Execute(int index, TransformAccess transform)
			{
				var data = Data[index];
				transform.SetLocalPositionAndRotation(data.Position, data.Rotation);
			}
		}
		
		private readonly Query m_transformsQuery;

		private readonly DataPool<TransformData> m_transformsData;
		private readonly DataPool<GameObjectData> m_gameObjects;

		private int m_lastGeneration = -1;

		private Transform[] m_transforms;
		private TransformData[] m_transformData;

		private NativeArray<TransformData> m_nativeTransformData;
		private TransformAccessArray m_accessArray;

		private bool m_disposed;

		public TransformSystem(World world)
		{
			m_transformsQuery = world.Query().Include<TransformData, GameObjectData>().Build();

			m_transformsData = world.GetPool<TransformData>();
			m_gameObjects = world.GetPool<GameObjectData>();
		}

		public void Tick(float deltaTime)
		{
			if (m_transformsQuery.Generation != m_lastGeneration) {
				m_lastGeneration = m_transformsQuery.Generation;
				PopulateCollections();
			}

			var index = 0;
			foreach (var entity in m_transformsQuery) {
				ref var transformData = ref m_transformsData.Get(entity);
				m_transformData[index++] = transformData;
			}

			var subArray = m_nativeTransformData.GetSubArray(0, m_transformsQuery.Count);
			subArray.CopyFrom(m_transformData);

			var job = new TransformSystemJob {
				Data = m_nativeTransformData
			};

			var jobHandle = job.Schedule(m_accessArray);
			jobHandle.Complete();
		}

		private void PopulateCollections()
		{
			Array.Resize(ref m_transformData, m_transformsQuery.Count);
			Array.Resize(ref m_transforms, m_transformsQuery.Count);

			var index = 0;
			foreach (var entity in m_transformsQuery) {
				ref var gameObjectData = ref m_gameObjects.Get(entity);
				m_transforms[index++] = gameObjectData.Transform;
			}

			if (!m_nativeTransformData.IsCreated || m_nativeTransformData.Length < m_transformsQuery.Count) {
				if (m_nativeTransformData.IsCreated) {
					m_nativeTransformData.Dispose();
				}

				m_nativeTransformData = new NativeArray<TransformData>(m_transformsQuery.Count, Allocator.Persistent);
			}
			
			if (!m_accessArray.isCreated) {
				m_accessArray = new TransformAccessArray(m_transforms);
			}

			if (m_accessArray.capacity < m_transformsQuery.Count) {
				m_accessArray.capacity = m_transformsQuery.Count;
			}
			
			m_accessArray.SetTransforms(m_transforms);
		}

		public void Dispose()
		{
			if (m_disposed) {
				return;
			}
			
			m_disposed = true;
			m_nativeTransformData.Dispose();
			m_accessArray.Dispose();
		}
	}
}