using System;

namespace Tekly.Stagecraft
{
	internal enum ListSlot
	{
		All,
		Tick,
		Fixed
	}

	/// <summary>
	/// Dense array with the element's index stored on the element, so add and remove are O(1) (swap-remove).
	/// Only Stage.Sync adds or removes, so a tick loop never sees the array change under it.
	/// </summary>
	internal sealed class ActorCollection<T> where T : ActorBehaviour
	{
		public T[] Items = new T[16];
		public int Count;

		private readonly ListSlot m_slot;

		public ActorCollection(ListSlot slot)
		{
			m_slot = slot;
		}

		public void Add(T behaviour)
		{
			if (Count == Items.Length) {
				Array.Resize(ref Items, Count * 2);
			}

			Items[Count] = behaviour;
			SetIndex(behaviour, Count);
			Count++;
		}

		public void Remove(T behaviour)
		{
			var index = GetIndex(behaviour);
			if (index < 0) {
				return;
			}

			var last = Count - 1;
			var moved = Items[last];
			Items[index] = moved;
			SetIndex(moved, index);
			Items[last] = null;
			Count = last;
			SetIndex(behaviour, -1);
		}

		private int GetIndex(T behaviour)
		{
			switch (m_slot) {
				case ListSlot.All:
					return behaviour.AllIndex;
				case ListSlot.Tick:
					return behaviour.TickIndex;
				default:
					return behaviour.FixedIndex;
			}
		}

		private void SetIndex(T behaviour, int index)
		{
			switch (m_slot) {
				case ListSlot.All:
					behaviour.AllIndex = index;
					break;
				case ListSlot.Tick:
					behaviour.TickIndex = index;
					break;
				default:
					behaviour.FixedIndex = index;
					break;
			}
		}
	}

	/// <summary>
	/// A view of every live behaviour of a registered type. Enumeration skips actors despawned since the last sync;
	/// <see cref="AsSpan"/> is the raw array for batch code (check IsAlive yourself, or copy to a NativeArray).
	/// </summary>
	public readonly struct ActorList<T> where T : ActorBehaviour
	{
		public static ActorList<T> Empty => default;

		/// <summary>Entries in the raw array, including ones whose actor is no longer alive.</summary>
		public int RawCount => m_collection?.Count ?? 0;

		private readonly ActorCollection<T> m_collection;

		internal ActorList(ActorCollection<T> collection)
		{
			m_collection = collection;
		}

		public ReadOnlySpan<T> AsSpan()
		{
			return m_collection == null ? ReadOnlySpan<T>.Empty : new ReadOnlySpan<T>(m_collection.Items, 0, m_collection.Count);
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(m_collection);
		}

		public struct Enumerator
		{
			private readonly T[] m_items;
			private readonly int m_count;
			private int m_index;

			internal Enumerator(ActorCollection<T> collection)
			{
				m_items = collection?.Items;
				m_count = collection?.Count ?? 0;
				m_index = -1;
			}

			public T Current => m_items[m_index];

			public bool MoveNext()
			{
				while (++m_index < m_count) {
					if (m_items[m_index].Actor.State == ActorState.Active) {
						return true;
					}
				}

				return false;
			}
		}
	}
}
