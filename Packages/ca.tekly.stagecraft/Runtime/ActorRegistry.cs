using System;
using System.Collections.Generic;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// Slot table from <see cref="ActorId"/> to live actor. Freeing a slot bumps its generation so stale ids miss.
	/// </summary>
	internal sealed class ActorRegistry
	{
		public int Count { get; private set; }
		public int Capacity => m_actors.Length;

		private Actor[] m_actors;
		private uint[] m_generations;
		private readonly Stack<int> m_free = new Stack<int>();

		public ActorRegistry(int capacity)
		{
			m_actors = new Actor[capacity];
			m_generations = new uint[capacity];

			for (var i = 0; i < capacity; i++) {
				m_generations[i] = 1;
			}

			for (var i = capacity - 1; i >= 0; i--) {
				m_free.Push(i);
			}
		}

		public ActorId Allocate(Actor actor)
		{
			if (m_free.Count == 0) {
				Grow();
			}

			var index = m_free.Pop();
			m_actors[index] = actor;
			Count++;

			return new ActorId(index, m_generations[index]);
		}

		public void Free(ActorId id)
		{
			if (!TryGet(id, out _)) {
				throw new InvalidOperationException($"Freeing an id that is not allocated: {id}");
			}

			m_actors[id.Index] = null;

			var generation = m_generations[id.Index] + 1;
			if (generation == 0) {
				generation = 1;
			}

			m_generations[id.Index] = generation;
			m_free.Push(id.Index);
			Count--;
		}

		public bool TryGet(ActorId id, out Actor actor)
		{
			if (id.IsNone || id.Index >= m_actors.Length || m_generations[id.Index] != id.Generation) {
				actor = null;
				return false;
			}

			actor = m_actors[id.Index];
			return !ReferenceEquals(actor, null);
		}

		/// <summary>The actor in a slot, or null. For whole-table walks (Clear), not per-frame use.</summary>
		public Actor GetAt(int index)
		{
			return m_actors[index];
		}

		private void Grow()
		{
			var oldCapacity = m_actors.Length;
			var newCapacity = oldCapacity * 2;

			Array.Resize(ref m_actors, newCapacity);
			Array.Resize(ref m_generations, newCapacity);

			for (var i = oldCapacity; i < newCapacity; i++) {
				m_generations[i] = 1;
			}

			for (var i = newCapacity - 1; i >= oldCapacity; i--) {
				m_free.Push(i);
			}
		}
	}
}
