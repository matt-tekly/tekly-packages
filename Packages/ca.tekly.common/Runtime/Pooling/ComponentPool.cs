using System.Collections.Generic;
using Tekly.Common.Utils;
// using Tekly.Logging;
using UnityEngine;

namespace Tekly.Common.Pooling
{
    public interface IComponentPool<T> where T : MonoBehaviour
    {
        T Get();
        void Clear();
    }
    
    public class ComponentPool<T> : IComponentPool<T> where T : MonoBehaviour
    {
        private readonly Stack<PooledComponent<T>> m_free = new Stack<PooledComponent<T>>();
        private readonly List<PooledComponent<T>> m_all = new List<PooledComponent<T>>();
        
        // private readonly TkLogger m_logger = TkLogger.Get(typeof(ComponentPool<>));
        private readonly PooledComponent<T> m_template;
        
        public ComponentPool(PooledComponent<T> template)
        {
            m_template = PrefabProtector.Protect(template);
        }
        
        public T Get()
        {
            if (m_free.Count != 0) {
                return m_free.Pop() as T;
            }

            var instance = m_template.Instantiate();
            m_all.Add(instance);

            instance.Pool = this;
            instance.PoolId = m_all.Count;
                
            if (Debug.isDebugBuild) {
                instance.Name = $"{m_template.Name} [{instance.PoolId:000}]";
            }
            
            return instance as T;
        }

        public void Return(PooledComponent<T> instance)
        {
            if (m_free.Contains(instance)) {
                // m_logger.Error("Returning PooledComponent multiple times - Type: [{type}]  Id: [{id}]", ("type", typeof(T).Name), ("id", instance.PoolId));
                Debug.LogError($"Returning PooledComponent multiple times - Type: [{typeof(T).Name}]  Id: [{instance.PoolId}]");
                return;
            }
            
            m_free.Push(instance);
        }

        public void Clear()
        {
            foreach (var poolable in m_all) {
                if (poolable != null && poolable.gameObject != null) {
                    Object.Destroy(poolable.gameObject);    
                }
            }
            
            m_all.Clear();
            m_free.Clear();
        }
    }
}