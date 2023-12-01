using System;
using System.Collections;
using System.Collections.Generic;

namespace Tekly.Sheets.Dynamics
{
	public enum DynamicType
	{
		Array,
		Object
	}
	
	public class Dynamic : IEnumerable<KeyValuePair<object, object>>
	{
		public readonly DynamicType Type;
		private readonly SortedDictionary<object, object> m_entries = new SortedDictionary<object, object>();

		public int Count => m_entries.Count;
		
		public Dynamic(DynamicType type)
		{
			Type = type;
		}
		
		public object this[object id] {
			get => m_entries[id];
			set => m_entries[id] = value;
		}
		
		public void Set(object[] path, object obj)
		{
			var currentObject = this;

			var lastIndex = path.Length - 1;
			
			for (var i = 0; i < lastIndex; i++) {
				var currentKey = path[i];
			
				if (!currentObject.m_entries.TryGetValue(currentKey, out var dataObj)) {
					var nextKey = path[i + 1];
					var isNumber = nextKey is int;
					
					var newObj = new Dynamic(isNumber ? DynamicType.Array : DynamicType.Object);
					currentObject[currentKey] = newObj;
					currentObject = newObj;
				} else {
					if (dataObj is Dynamic dyn) {
						currentObject = dyn;
					} else {
						throw new Exception("Expected a Dynamic but got a " + dataObj.GetType().Name);
					}
				}
			}
			
			currentObject[path[lastIndex]] = obj;
		}
		
		public bool TryGet<T>(object[] path, out T value)
		{
			var currentObject = this;

			var lastIndex = path.Length - 1;
			
			for (var i = 0; i < lastIndex; i++) {
				var currentKey = path[i];
			
				if (!currentObject.m_entries.TryGetValue(currentKey, out var dataObj)) {
					value = default;
					return false;
				}

				if (dataObj is Dynamic dyn) {
					currentObject = dyn;
				} else {
					value = default;
					return false;
				}
			}
			
			return currentObject.TryGet(path[lastIndex], out value);
		}

		public bool TryGet<T>(object key, out T value)
		{
			if (m_entries.TryGetValue(key, out var target)) {
				value = (T) target;
				return true;
			}

			value = default;
			return false;
		}
        
        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
	        return m_entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
	        return GetEnumerator();
        }
	}
}