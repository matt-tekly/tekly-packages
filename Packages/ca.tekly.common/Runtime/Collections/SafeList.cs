using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tekly.Common.Collections
{
	/// <summary>
	/// A list that handles adding and removing elements while iterating over it. Loops using the
	/// foreach keyword, the ForEach() method, or the GetEnumerator() methods will be internally tracked
	/// by this class. Adding and removing elements during these loops will not cause exceptions to be
	/// thrown, unlike with System.Collections.List{T}.
	/// 
	/// When elements are added after the current loop position, the loop will
	/// proceed to them as though they were present from the start. Elements added
	/// before the current loop position will be skipped. Elements removed before
	/// the current loop position will have no effect on the loop. Elements removed
	/// after the current loop position will not be iterated over.
	/// </summary>
	/// <author>Jackson Dunstan, http://JacksonDunstan.com/articles/3179</author>
	/// <license>MIT</license>
	public class SafeList<T>
		: IList<T>, IList, ICollection<T>, IEnumerable<T>, IEnumerable, ICollection
	{
		private readonly List<T> list;
		private readonly List<int?> iterationIndices = new List<int?>();

		public SafeList()
		{
			list = new List<T>();
		}

		public SafeList(int capacity)
		{
			list = new List<T>(capacity);
		}

		public SafeList(IEnumerable<T> collection)
		{
			list = new List<T>(collection);
		}

		public int Capacity
		{
			get { return list.Capacity; }
			set { list.Capacity = value; }
		}

		public int Count
		{
			get { return list.Count; }
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}

		bool IList.IsReadOnly
		{
			get { return false; }
		}

		bool ICollection.IsSynchronized
		{
			get { return false; }
		}

		object ICollection.SyncRoot
		{
			get { return this; }
		}

		public T this[int index]
		{
			get { return list[index]; }
			set { list[index] = value; }
		}

		object IList.this[int index]
		{
			get { return list[index]; }
			set { list[index] = (T) value; }
		}

		int IList.Add(object value)
		{
			var index = list.Count;
			list.Add((T) value);
			return index;
		}

		public void Add(T value)
		{
			list.Add(value);
		}

		public void AddRange(IEnumerable<T> collection)
		{
			list.AddRange(collection);
		}

		public ReadOnlyCollection<T> AsReadOnly()
		{
			return list.AsReadOnly();
		}

		public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
		{
			return list.BinarySearch(index, count, item, comparer);
		}

		public int BinarySearch(T item, IComparer<T> comparer)
		{
			return list.BinarySearch(item, comparer);
		}

		public int BinarySearch(T item)
		{
			return list.BinarySearch(item);
		}

		public void Clear()
		{
			list.Clear();
			for (int i = 0, len = iterationIndices.Count; i < len; ++i) {
				var iterationIndex = iterationIndices[i];
				if (iterationIndex != null) {
					iterationIndices[i] = 0;
				}
			}
		}

		bool IList.Contains(object value)
		{
			return list.Contains((T) value);
		}

		public bool Contains(T value)
		{
			return list.Contains(value);
		}

		public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
		{
			return list.ConvertAll(converter);
		}

		public void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			list.CopyTo(index, array, arrayIndex, count);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			list.CopyTo(array, arrayIndex);
		}

		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			list.CopyTo((T[]) array, arrayIndex);
		}

		public void CopyTo(T[] array)
		{
			list.CopyTo(array);
		}

		public bool Exists(Predicate<T> match)
		{
			return list.Exists(match);
		}

		public T Find(Predicate<T> match)
		{
			return list.Find(match);
		}

		public List<T> FindAll(Predicate<T> match)
		{
			return list.FindAll(match);
		}

		public int FindIndex(int startIndex, int count, Predicate<T> match)
		{
			return list.FindIndex(startIndex, count, match);
		}

		public int FindIndex(int startIndex, Predicate<T> match)
		{
			return list.FindIndex(startIndex, match);
		}

		public int FindIndex(Predicate<T> match)
		{
			return list.FindIndex(match);
		}

		public T FindLast(Predicate<T> match)
		{
			return list.FindLast(match);
		}

		public int FindLastIndex(int startIndex, int count, Predicate<T> match)
		{
			return list.FindLastIndex(startIndex, count, match);
		}

		public int FindLastIndex(Predicate<T> match)
		{
			return list.FindLastIndex(match);
		}

		public int FindLastIndex(int startIndex, Predicate<T> match)
		{
			return list.FindLastIndex(startIndex, match);
		}

		public void ForEach(Action<T> action)
		{
			var ii = UseIterationIndex();
			var arrayIndex = 0;
			while (arrayIndex < list.Count) {
				var cur = list[arrayIndex];
				action(cur);
				iterationIndices[ii] = arrayIndex = iterationIndices[ii].Value + 1;
			}

			iterationIndices[ii] = null;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			var ii = UseIterationIndex();
			var arrayIndex = 0;
			while (arrayIndex < list.Count) {
				var cur = list[arrayIndex];
				yield return cur;
				iterationIndices[ii] = arrayIndex = iterationIndices[ii].Value + 1;
			}

			iterationIndices[ii] = null;
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			var ii = UseIterationIndex();
			var arrayIndex = 0;
			while (arrayIndex < list.Count) {
				var cur = list[arrayIndex];
				yield return cur;
				iterationIndices[ii] = arrayIndex = iterationIndices[ii].Value + 1;
			}

			iterationIndices[ii] = null;
		}

		public IEnumerator<T> GetEnumerator()
		{
			var ii = UseIterationIndex();
			var arrayIndex = 0;
			while (arrayIndex < list.Count) {
				var cur = list[arrayIndex];
				yield return cur;
				iterationIndices[ii] = arrayIndex = iterationIndices[ii].Value + 1;
			}

			iterationIndices[ii] = null;
		}

		public List<T> GetRange(int index, int count)
		{
			return list.GetRange(index, count);
		}

		int IList.IndexOf(object value)
		{
			return list.IndexOf((T) value);
		}

		public int IndexOf(T item, int index, int count)
		{
			return list.IndexOf(item, index, count);
		}

		public int IndexOf(T item, int index)
		{
			return list.IndexOf(item, index);
		}

		public int IndexOf(T value)
		{
			return list.IndexOf(value);
		}

		public void Insert(int index, T value)
		{
			list.Insert(index, value);
			for (int i = 0, len = iterationIndices.Count; i < len; ++i) {
				var iterationIndex = iterationIndices[i];
				if (iterationIndex != null) {
					var iterationIndexVal = iterationIndex.Value;
					if (index <= iterationIndexVal) {
						iterationIndices[i] = iterationIndexVal + 1;
					}
				}
			}
		}

		void IList.Insert(int index, object value)
		{
			Insert(index, (T) value);
		}

		public int LastIndexOf(T item, int index, int count)
		{
			return list.LastIndexOf(item, index, count);
		}

		public int LastIndexOf(T item, int index)
		{
			return list.LastIndexOf(item, index);
		}

		public int LastIndexOf(T item)
		{
			return list.LastIndexOf(item);
		}

		public bool Remove(T value)
		{
			var index = list.IndexOf(value);
			if (index < 0 || index >= list.Count) {
				return false;
			}

			RemoveAt(index);
			return true;
		}

		void IList.Remove(object value)
		{
			Remove((T) value);
		}

		public int RemoveAll(Predicate<T> match)
		{
			var numRemoved = 0;
			for (var i = 0; i < list.Count; ++i) {
				if (match(list[i])) {
					RemoveAt(i);
					i--;
					numRemoved++;
				}
			}

			return numRemoved;
		}

		public void RemoveAt(int index)
		{
			list.RemoveAt(index);
			for (int i = 0, len = iterationIndices.Count; i < len; ++i) {
				var iterationIndex = iterationIndices[i];
				if (iterationIndex != null) {
					var iterationIndexVal = iterationIndex.Value;
					if (iterationIndexVal >= index) {
						iterationIndices[i] = iterationIndexVal - 1;
					}
				}
			}
		}

		public void RemoveRange(int index, int count)
		{
			for (int i = 0; i < count; ++i) {
				RemoveAt(index);
			}
		}

		public void Reverse(int index, int count)
		{
			list.Reverse(index, count);
		}

		public void Reverse()
		{
			list.Reverse();
		}

		public void Sort()
		{
			list.Sort();
		}

		public void Sort(IComparer<T> comparer)
		{
			list.Sort(comparer);
		}

		public void Sort(Comparison<T> comparison)
		{
			list.Sort(comparison);
		}

		public void Sort(int index, int count, IComparer<T> comparer)
		{
			list.Sort(index, count, comparer);
		}

		public T[] ToArray()
		{
			return list.ToArray();
		}

		public List<T> ToList()
		{
			return new List<T>(list);
		}

		public void TrimExcess()
		{
			list.TrimExcess();
		}

		public bool TrueForAll(Predicate<T> match)
		{
			return list.TrueForAll(match);
		}

		private int UseIterationIndex()
		{
			for (int i = 0, len = iterationIndices.Count; i < len; ++i) {
				if (iterationIndices[i] == null) {
					iterationIndices[i] = 0;
					return i;
				}
			}

			var index = iterationIndices.Count;
			iterationIndices.Add(0);
			return index;
		}
	}
}