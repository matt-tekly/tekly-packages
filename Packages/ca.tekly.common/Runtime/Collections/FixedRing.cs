using System;
using System.Runtime.CompilerServices;

namespace Tekly.Common.Collections
{
	public class FixedRing<T>
	{
		public int Count { get; private set; }

		private readonly T[] m_array;
		private int m_index;
		
		public FixedRing(int capacity)
		{
			m_array = new T[capacity];
		}

		public void Add(T value)
		{
			Count = Math.Min(m_array.Length, Count + 1);
			
			m_array[m_index] = value;
			m_index = (m_index + 1) % m_array.Length;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}
		
		public struct Enumerator : IDisposable
		{
			private readonly T[] m_data;
			private readonly int m_count;
			private int m_idx;
			private int m_remaining;

			public Enumerator(FixedRing<T> ring)
			{
				m_data = ring.m_array;
				m_count = ring.Count;
				m_remaining = m_count;
				m_idx = ring.m_index-1;
			}

			public T Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => m_data[m_idx];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				m_idx = (m_idx + 1) % m_count;
				m_remaining--;
				return m_remaining >= 0;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Dispose()
			{
				// Do Nothing
			}
		}
	}
}