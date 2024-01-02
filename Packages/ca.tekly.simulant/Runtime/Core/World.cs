#if DEBUG
#define SIMULANT_ASSERTS
#endif

using System;
using System.Collections.Generic;
using Tekly.Simulant.Collections;
using Tekly.SuperSerial.Serialization;
using UnityEngine.Assertions;

namespace Tekly.Simulant.Core
{
	public partial class World : ISuperSerialize
	{
		public readonly GrowingArray<EntityData> Entities;
		public int EntityCapacity => Entities.Data.Length;
		public int DataCapacity => m_pools.Data.Length;

		private readonly GrowingArray<int> m_recycledEntities;

		private readonly WorldConfig m_config;

		private readonly GrowingArray<IDataPool> m_pools;
		private readonly Dictionary<Type, IDataPool> m_poolMap;

		private readonly GrowingArray<Query> m_queries = new GrowingArray<Query>(64);
		private readonly Dictionary<int, Query> m_queryMap = new Dictionary<int, Query>();

		private readonly GrowingArray<List<Query>> m_queriesIncluding;
		private readonly GrowingArray<List<Query>> m_queriesExcluding;

		private readonly QueryBuilder m_queryBuilder;

		private const int DATA_TYPE_CAPACITY = 512;

		public World(WorldConfig config)
		{
			m_config = config;
			Entities = new GrowingArray<EntityData>(m_config.EntityCapacity);
			m_recycledEntities = new GrowingArray<int>(m_config.EntityCapacity);

			m_pools = new GrowingArray<IDataPool>(DATA_TYPE_CAPACITY);
			m_poolMap = new Dictionary<Type, IDataPool>(DATA_TYPE_CAPACITY);

			m_queriesIncluding = new GrowingArray<List<Query>>(DATA_TYPE_CAPACITY);
			m_queriesExcluding = new GrowingArray<List<Query>>(DATA_TYPE_CAPACITY);

			m_queryBuilder = new QueryBuilder(this);
		}

		public bool IsAlive(int entity)
		{
			return entity >= 0 && entity < Entities.Count && Entities.Data[entity].Generation > 0;
		}

		public QueryBuilder Query()
		{
			m_queryBuilder.Start();
			return m_queryBuilder;
		}

		internal Query FinalizeQuery(QueryBuilder queryBuilder)
		{
			var hash = queryBuilder.Hash;
			if (m_queryMap.TryGetValue(hash, out var query)) {
				queryBuilder.Reset();
				return query;
			}

			var (includes, excludes) = queryBuilder.Compact();

			query = new Query(includes, excludes, m_config.EntityCapacity, EntityCapacity);

			m_queries.Add(query);
			m_queryMap[hash] = query;

			for (int index = 0, length = includes.Length; index < length; index++) {
				var include = includes[index];
				var list = m_queriesIncluding.Data[include];
				if (list == null) {
					list = new List<Query>(16);
					m_queriesIncluding.Data[include] = list;
				}

				list.Add(query);
			}

			for (int index = 0, length = excludes.Length; index < length; index++) {
				var exclude = excludes[index];
				var list = m_queriesExcluding.Data[exclude];
				if (list == null) {
					list = new List<Query>(16);
					m_queriesExcluding.Data[exclude] = list;
				}

				list.Add(query);
			}

			for (int entity = 0, length = Entities.Count; entity < length; entity++) {
				ref var entityData = ref Entities.Data[entity];
				if (entityData.ComponentsCount > 0 && query.Matches(m_pools, entity)) {
					query.Add(entity);
				}
			}

			return query;
		}

		public void EntityDataChanged(int entity, int id) { }

		public int Create()
		{
			int entity;
			if (m_recycledEntities.Count > 0) {
				entity = m_recycledEntities.Pop();
				ref var entityData = ref Entities.Data[entity];
				entityData.Generation = (short)-entityData.Generation;
			} else {
				if (Entities.IsFull) {
					var newSize = Entities.Data.Length << 1;
					Entities.Resize(newSize);

					for (int i = 0, length = m_pools.Count; i < length; i++) {
						m_pools.Data[i].Resize(newSize);
					}

					for (int i = 0, length = m_queries.Count; i < length; i++) {
						m_queries.Data[i].ResizeEntityMap(newSize);
					}
				}

				entity = Entities.Get();
				Entities.Data[entity].Generation = 1;
			}

			return entity;
		}

		public void Delete(int entity)
		{
			Assert.IsTrue(IsAlive(entity));

			ref var entityData = ref Entities.Data[entity];
			if (entityData.Generation < 0) {
				return;
			}

			if (entityData.ComponentsCount > 0) {
				var idx = 0;
				while (entityData.ComponentsCount > 0 && idx < m_pools.Count) {
					for (; idx < m_pools.Count; idx++) {
						if (m_pools.Data[idx].Has(entity)) {
							m_pools.Data[idx++].Delete(entity);
							break;
						}
					}
				}

#if SIMULANT_ASSERTS
				if (entityData.ComponentsCount != 0) {
					throw new Exception(
						$"Invalid components count on entity [{entity}] [{entityData.ComponentsCount}].");
				}
#endif
				return;
			}

			entityData.Generation =
				(short)(entityData.Generation == short.MaxValue ? -1 : -(entityData.Generation + 1));
			m_recycledEntities.Add(entity);
		}

		public void OnEntityChangeInternal(int entity, int typeId, Modification modification)
		{
			var includeList = m_queriesIncluding.Data[typeId];
			var excludeList = m_queriesExcluding.Data[typeId];

			switch (modification) {
				// The data has been added to the pool before this function is called
				case Modification.Add:
					if (includeList != null) {
						for (int index = 0, length = includeList.Count; index < length; index++) {
							var query = includeList[index];
							if (query.Matches(m_pools, entity)) {
								query.Add(entity);
							}
						}
					}

					if (excludeList != null) {
						for (int index = 0, length = excludeList.Count; index < length; index++) {
							var query = excludeList[index];
							if (query.MatchesWithoutType(m_pools, entity, typeId)) {
								query.Remove(entity);
							}
						}
					}

					break;
				// The data will be removed from the pool after this function is called
				case Modification.Remove:
					if (includeList != null) {
						for (int index = 0, length = includeList.Count; index < length; index++) {
							var query = includeList[index];
							if (query.Matches(m_pools, entity)) {
								query.Remove(entity);
							}
						}
					}

					if (excludeList != null) {
						for (int index = 0, length = excludeList.Count; index < length; index++) {
							var query = excludeList[index];
							if (query.MatchesWithoutType(m_pools, entity, typeId)) {
								query.Add(entity);
							}
						}
					}

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(modification), modification, null);
			}
		}

		public void Add<T>(int entity, ref T data) where T : struct
		{
			var pool = GetPool<T>();
			pool.Add(ref data, entity);
		}

		public void Add<T>(int entity, T data) where T : struct
		{
			var pool = GetPool<T>();
			pool.Add(ref data, entity);
		}

		public ref T Add<T>(int entity) where T : struct
		{
			var pool = GetPool<T>();
			return ref pool.Add(entity);
		}

		public ref T Get<T>(int entity) where T : struct
		{
			var pool = GetPool<T>();
			return ref pool.Get(entity);
		}

		public DataPool<T> GetPool<T>() where T : struct
		{
			var poolType = typeof(T);

			if (m_poolMap.TryGetValue(poolType, out var rawPool)) {
				return (DataPool<T>)rawPool;
			}

			var pool = new DataPool<T>(this, m_pools.Count, EntityCapacity, m_config.DataPools);
			m_poolMap[poolType] = pool;

			if (m_pools.Count == m_pools.Data.Length) {
				var newSize = m_pools.Count << 1;
				m_pools.Resize(newSize);
				m_queriesIncluding.Resize(newSize);
				m_queriesExcluding.Resize(newSize);
			}

			m_pools.Add(pool);

			return pool;
		}
	}
}