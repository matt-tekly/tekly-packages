using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Common.Utils
{
    public class SingletonRegistry<T, TSingleton> : Singleton<TSingleton> where TSingleton : Singleton<TSingleton>, new()
    {
        private readonly Dictionary<string, T> m_objects = new Dictionary<string, T>();
        
        public void Register(string id, T obj)
        {
#if DEBUG
            if (m_objects.ContainsKey(id)) {
                Debug.LogError($"Trying to register [{id}] when a [{typeof(T).Name}] with that id already exists.");        
            }
#endif
            m_objects[id] = obj;
        }
        
        public void Remove(string name)
        {
            m_objects.Remove(name);
        }

        public bool TryGet(string name, out T value)
        {
            return m_objects.TryGetValue(name, out value);
        }
        
        public bool TryGet<TV>(string name, out TV value) where TV : class, T
        {
            if (m_objects.TryGetValue(name, out var target)) {
                value = target as TV;
                return value != null;
            }

            value = default;
            return false;
        }
    }
}