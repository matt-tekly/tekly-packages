using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Tekly.Simulant.Collections;

namespace Tekly.Simulant.Core
{
	public interface IQueryListener
	{
		void EntityAdded(int entity, Query query);
		void EntityRemoved(int entity, Query query);
	}

	public partial class Query
	{
		public int Count => m_entities.Count;
		public int Generation => m_generation;

		public readonly int[] Includes;
		public readonly int[] Excludes;

		private readonly IndexArray m_entities;
		private readonly IndexArray m_entityMap;

		private readonly GrowingArray<DelayedOp> m_delayedOps;

		private readonly List<IQueryListener> m_listeners = new List<IQueryListener>();

		private int m_lockCount;
		private int m_generation;

		private const int BAD_ID = -1;

		public Query(int[] includes, int[] excludes, int entityCapacity, int mapCapacity)
		{
			Includes = includes;
			Excludes = excludes;

			m_entities = new IndexArray(entityCapacity, BAD_ID);
			m_entityMap = new IndexArray(mapCapacity, BAD_ID);

			m_delayedOps = new GrowingArray<DelayedOp>(512);
		}

		public void AddListener(IQueryListener queryListener)
		{
			m_listeners.Add(queryListener);
		}

		public void RemoveListener(IQueryListener queryListener)
		{
			m_listeners.Remove(queryListener);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IndexArray GetRawEntities()
		{
			return m_entities;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ResizeEntityMap(int capacity)
		{
			m_entityMap.Resize(capacity);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Enumerator GetEnumerator()
		{
			m_lockCount++;
			return new Enumerator(this);
		}

		public bool Matches(GrowingArray<IDataPool> pools, int entity)
		{
			for (int index = 0, length = Includes.Length; index < length; index++) {
				var dataType = Includes[index];
				if (!pools.Data[dataType].Has(entity)) {
					return false;
				}
			}

			for (int index = 0, length = Excludes.Length; index < length; index++) {
				var dataType = Excludes[index];
				if (pools.Data[dataType].Has(entity)) {
					return false;
				}
			}

			return true;
		}

		public bool MatchesWithoutType(GrowingArray<IDataPool> pools, int entity, int removedType)
		{
			for (int i = 0, length = Includes.Length; i < length; i++) {
				var typeId = Includes[i];
				if (typeId == removedType || !pools.Data[typeId].Has(entity)) {
					return false;
				}
			}

			for (int i = 0, length = Excludes.Length; i < length; i++) {
				var typeId = Excludes[i];
				if (typeId != removedType && pools.Data[typeId].Has(entity)) {
					return false;
				}
			}

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(int entity)
		{
			// TODO: Validate it wasn't already added
			m_generation++;

			if (ShouldDelay(Modification.Add, entity)) {
				return;
			}

			m_entityMap.Data[entity] = m_entities.Add(entity);

			ProcessModificationEvents(entity, Modification.Add);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Remove(int entity)
		{
			// TODO: Validate it wasn't already removed
			m_generation++;

			if (ShouldDelay(Modification.Remove, entity)) {
				return;
			}

			var entityIndex = m_entityMap.Data[entity];

			m_entityMap.Data[entity] = BAD_ID;
			var lastEntity = m_entities.Pop();

			// If this isn't the last element then fill the hole with the last element
			if (entityIndex < m_entities.Count) {
				m_entities.Data[entityIndex] = lastEntity;
				m_entityMap.Data[lastEntity] = entityIndex;
			}

			ProcessModificationEvents(entity, Modification.Remove);
		}

		public void ForEach(Action<int> action)
		{
			m_lockCount++;
			for (int i = 0, count = m_entities.Count; i < count; i++) {
				action(m_entities.Data[i]);
			}
			Unlock();
		}

		private void ProcessModificationEvents(int entity, Modification modification)
		{
			if (modification == Modification.Add) {
				for (var index = m_listeners.Count - 1; index >= 0; index--) {
					var listener = m_listeners[index];
					listener.EntityAdded(entity, this);
				}
			} else {
				for (var index = m_listeners.Count - 1; index >= 0; index--) {
					var listener = m_listeners[index];
					listener.EntityRemoved(entity, this);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool ShouldDelay(Modification modification, int entity)
		{
			if (m_lockCount <= 0) {
				return false;
			}

			m_delayedOps.Add(new DelayedOp {
				Entity = entity,
				Modification = modification
			});

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Unlock()
		{
#if DEBUG
			if (m_lockCount <= 0) {
				throw new Exception($"Invalid lock/unlock balance for [{GetType().Name}]");
			}
#endif
			m_lockCount--;
			if (m_lockCount != 0 || m_delayedOps.Count == 0) {
				return;
			}

			for (int i = 0, length = m_delayedOps.Count; i < length; i++) {
				ref var op = ref m_delayedOps.Data[i];
				if (op.Modification == Modification.Add) {
					Add(op.Entity);
				} else {
					Remove(op.Entity);
				}
			}

			m_delayedOps.Count = 0;
		}

		public struct Enumerator : IDisposable
		{
			private readonly Query m_query;
			private readonly int[] m_entities;
			private readonly int m_count;
			private int m_idx;

			public Enumerator(Query query)
			{
				m_query = query;
				m_entities = query.m_entities.Data;
				m_count = query.m_entities.Count;
				m_idx = -1;
			}

			public int Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => m_entities[m_idx];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				return ++m_idx < m_count;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Dispose()
			{
				m_query.Unlock();
			}
		}

		private struct DelayedOp
		{
			public Modification Modification;
			public int Entity;
		}
	}
}