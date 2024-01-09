using System;
using System.Runtime.CompilerServices;

namespace Tekly.Simulant.Collections
{
	public class IndexArray<T>
	{
		public T[] Data;
		public int Count;

		public bool IsFull => Count == Data.Length;

		private readonly T m_defaultValue;
		
		public IndexArray(int capacity, T defaultValue)
		{
			Data = new T[capacity];
			m_defaultValue = defaultValue;
			Count = 0;
			
			Array.Fill(Data, m_defaultValue);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Add(T data)
		{
			var index = Count;
			if (Count == Data.Length) {
				Array.Resize(ref Data, Count << 1);
			}

			Data[index] = data;
			Count++;

			return index;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Pop()
		{
			var value = Data[--Count];
			Data[Count] = m_defaultValue;
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Get()
		{
			var index = Count;
		
			if (Count == Data.Length) {
				Array.Resize (ref Data, Count << 1);
			}
		
			Count++;

			return index;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Resize(int capacity)
		{
			var startCount = Data.Length; 
			Array.Resize(ref Data, capacity);
			Array.Fill(Data, m_defaultValue, startCount, capacity - startCount);
		}
	}
}