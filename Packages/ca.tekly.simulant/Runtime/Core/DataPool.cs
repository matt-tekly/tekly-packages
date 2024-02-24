#if DEBUG && !SIMULANT_PERFORMANCE
#define SIMULANT_ASSERTS
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Tekly.Common.Utils;
using Tekly.Simulant.Collections;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Tekly.Simulant.Core
{
	public class TypeInfo
	{
		public readonly string Assembly;
		public readonly string Name;
		public readonly Type Type;
		public readonly bool Transient;
		public readonly bool Init;
		public readonly bool Recycle;
		public readonly bool AutoRecycle;

		public TypeInfo(Type type)
		{
			Type = type;
			Assembly = type.AssemblyQualifiedName;
			Name = type.Name;
			Transient = type.Implements<ITransient>();
			Init = type.Implements<IInit>();
			Recycle = type.Implements<IRecycle>();
			AutoRecycle = type.Implements<IAutoRecycle>();
		}

		public static TypeInfo Create<T>() => new TypeInfo(typeof(T));
	}

	public interface IDataPool
	{
		Type DataType { get; }
		TypeInfo TypeInfo { get; }
		int Count { get; }
		bool ShouldSerialize { get; }
		void Resize(int capacity);
		bool Has(int entity);
		void Delete(int entity);

		void Write(TokenOutputStream output, SuperSerializer superSerializer);
		void Read(TokenInputStream input, SuperSerializer superSerializer);

		DataPoolSummary GetSummary();
	}

	public partial class DataPool<T> : IDataPool where T : struct
	{
		public readonly int Id;
		public Type DataType => typeof(T);

		public TypeInfo TypeInfo => m_typeInfo ??= TypeInfo.Create<T>();
		public int Count => m_data.Count - m_recycled.Count;

		public bool ShouldSerialize => m_data.Count > 0 && !TypeInfo.Transient;

		private readonly World m_world;
		private readonly GrowingArray<T> m_data;
		private readonly IndexArray m_entityMap;
		private readonly IndexArray m_recycled;

		private const int BAD_ID = -1;
		private TypeInfo m_typeInfo;
		
		private readonly InitDelegate<T> m_initDelegate;
		private readonly RecycleDelegate<T> m_recycleDelegate;
		private readonly bool m_autoRecycle;

		public DataPool(World world, int id, int entityCapacity, DataPoolConfig config)
		{
			m_world = world;
			Id = id;

			m_data = new GrowingArray<T>(entityCapacity);
			m_entityMap = new IndexArray(entityCapacity, BAD_ID);
			m_recycled = new IndexArray(config.RecycleCapacity, BAD_ID);

			if (typeof(T).Implements<IInit>()) {
				m_initDelegate = DataInit<T>.Init;
			}
			
			if (typeof(T).Implements<IRecycle>()) {
				m_recycleDelegate = DataRecycle<T>.Recycle;
			} else if(typeof(T).Implements<IAutoRecycle>()) {
				m_autoRecycle = true;
			}
		}

		public void Resize(int capacity)
		{
			m_entityMap.Resize(capacity);
		}

		public void Add(ref T data, int entity)
		{
			AssertAlive(entity);
			AssertNotExists(entity);

			var idx = m_recycled.Count > 0 ? m_recycled.Pop() : m_data.Get();

			m_entityMap.Data[entity] = idx;
			m_data.Data[idx] = data;
			
			m_world.OnEntityChangeInternal(entity, Id, Modification.Add);
			
			IncreaseComponentCount(ref m_world.Entities.Data[entity]);

			m_world.EntityDataChanged(entity, Id);
		}
		
		public void Add(int entity, T data)
		{
			AssertAlive(entity);
			AssertNotExists(entity);

			var idx = m_recycled.Count > 0 ? m_recycled.Pop() : m_data.Get();

			m_entityMap.Data[entity] = idx;
			m_data.Data[idx] = data;
			
			m_world.OnEntityChangeInternal(entity, Id, Modification.Add);
			
			IncreaseComponentCount(ref m_world.Entities.Data[entity]);

			m_world.EntityDataChanged(entity, Id);
		}

		public ref T Add(int entity)
		{
			AssertAlive(entity);
			AssertNotExists(entity);

			int idx;
			if (m_recycled.Count > 0) {
				idx = m_recycled.Pop();
			} else {
				idx = m_data.Get();
				InitData(ref m_data.Data[idx]);
			}

			m_entityMap.Data[entity] = idx;
			m_world.OnEntityChangeInternal(entity, Id, Modification.Add);
			
			IncreaseComponentCount(ref m_world.Entities.Data[entity]);

			m_world.EntityDataChanged(entity, Id);

			return ref m_data.Data[idx];
		}

		public void Delete(int entity)
		{
			AssertAlive(entity);

			ref var dataIndex = ref m_entityMap.Data[entity];
			if (dataIndex == BAD_ID) {
				return;
			}

			m_world.OnEntityChangeInternal(entity, Id, Modification.Remove);

			RecycleData(ref m_data.Data[dataIndex]);
			
			if (dataIndex == m_data.Count - 1) {
				m_data.Count--;	
			} else {
				m_recycled.Add(dataIndex);
			}
			
			dataIndex = BAD_ID;
			
			ref var entityData = ref m_world.Entities.Data[entity];
			DecreaseComponentCount(ref entityData);

			m_world.EntityDataChanged(entity, Id);

			if (entityData.ComponentsCount == 0) {
				m_world.Delete(entity);
			}
		}

		public DataPoolSummary GetSummary()
		{
			var summary = new DataPoolSummary();
			summary.Type = typeof(T).Name;
			summary.Blittable = UnsafeUtility.IsBlittable<T>();
			summary.Size = UnsafeUtility.SizeOf<T>();
			summary.Transient = TypeInfo.Transient;
			summary.Init = m_initDelegate != null;
			summary.Recycle = m_recycleDelegate != null;
			summary.AutoRecycle = m_autoRecycle;
			summary.Count = Count;

			return summary;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T Get(int entity)
		{
			AssertAlive(entity);
			AssertExists(entity);

			return ref m_data.Data[m_entityMap.Data[entity]];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Has(int entity)
		{
			AssertAlive(entity);
			return m_entityMap.Data[entity] != BAD_ID;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InitData(ref T data)
		{
			if (m_initDelegate != null) {
				m_initDelegate.Invoke(ref data);
			}
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RecycleData(ref T data)
		{
			if (m_recycleDelegate != null) {
				m_recycleDelegate.Invoke(ref data);
			} else if (m_autoRecycle) {
				data = default;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void IncreaseComponentCount(ref EntityData entityData)
		{
			entityData.ComponentsCount++;
			
			if (!TypeInfo.Transient) {
				entityData.PersistentComponents++;
			}
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DecreaseComponentCount(ref EntityData entityData)
		{
			entityData.ComponentsCount--;
			
			if (!TypeInfo.Transient) {
				entityData.PersistentComponents--;
			}
		}

		[Conditional("SIMULANT_ASSERTS")]
		private void AssertAlive(int entity)
		{
			Assert.IsTrue(m_world.IsAlive(entity), "Touching destroyed Entity");
		}

		[Conditional("SIMULANT_ASSERTS")]
		private void AssertNotExists(int entity)
		{
			if (m_entityMap.Data[entity] != BAD_ID) {
				throw new Exception($"Component [{TypeInfo.Name}] already exists on Entity");
			}
		}

		[Conditional("SIMULANT_ASSERTS")]
		private void AssertExists(int entity)
		{
			if (m_entityMap.Data[entity] == BAD_ID) {
				throw new Exception($"Component [{TypeInfo.Name}] does not exist on Entity");
			}
		}
	}
}