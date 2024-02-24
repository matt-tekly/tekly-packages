using System;

namespace Tekly.Simulant.Collections
{
	public class GrowingArray<T>
	{
		public T[] Data;
		public int Count;

		public bool IsFull => Count == Data.Length;
		
		public GrowingArray(int capacity)
		{
			Data = new T[capacity];
			Count = 0;
		}
		
		public int Add(T data)
		{
			var index = Count;
			if (Count == Data.Length) {
				Array.Resize(ref Data, Count * 2);
			}

			Data[index] = data;
			Count++;

			return index;
		}

		public ref T Pop()
		{
			return ref Data[--Count];
		}
		
		public int Get()
		{
			var index = Count;
			
			if (Count == Data.Length) {
				Array.Resize (ref Data, Count * 2);
			}
			
			Count++;

			return index;
		}

		public void Resize(int capacity)
		{
			Array.Resize(ref Data, capacity);
		}
	}
}