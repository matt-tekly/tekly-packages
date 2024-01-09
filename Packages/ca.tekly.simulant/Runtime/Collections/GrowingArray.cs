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
				Array.Resize(ref Data, Count << 1);
			}

			Data[index] = data;
			Count++;

			return index;
		}

		public T Pop()
		{
			return Data[--Count];
		}

		public int Get()
		{
			var index = Count;
			
			if (Count == Data.Length) {
				Array.Resize (ref Data, Count << 1);
			}
			
			Count++;

			return index;
		}

		public void Resize(int capacity)
		{
			Array.Resize(ref Data, capacity);
		}

		public T[] Compacted()
		{
			var items = new T[Count];
			Array.Copy(Data, items, Count);

			return items;
		}

		public bool Contains(T value)
		{
			return Array.IndexOf(Data, value, 0, Count) >= 0;
		}
	}
}