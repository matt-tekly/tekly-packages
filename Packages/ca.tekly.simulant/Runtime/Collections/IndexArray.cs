using System;
using System.Runtime.CompilerServices;

namespace Tekly.Simulant.Collections
{
	public class IndexArray
	{
		public int[] Data;
		public int Count;

		public bool IsFull => Count == Data.Length;

		private readonly int m_defaultValue;
		
		public IndexArray(int capacity, int defaultValue)
		{
			Data = new int[capacity];
			m_defaultValue = defaultValue;
			Count = 0;
			
			Array.Fill(Data, m_defaultValue);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Add(int data)
		{
			var index = Count;
			if (Count == Data.Length) {
				Array.Resize(ref Data, Count * 2);
			}

			Data[index] = data;
			Count++;

			return index;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Pop()
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
				Array.Resize (ref Data, Count * 2);
			}
		
			Count++;

			return index;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Resize(int capacity)
		{
			if (capacity <= Data.Length) {
				return;
			}
			
			var startCount = Data.Length; 
			Array.Resize(ref Data, capacity);
			Array.Fill(Data, m_defaultValue, startCount, capacity - startCount);
		}
	}
}