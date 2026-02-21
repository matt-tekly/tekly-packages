using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Common.Poolables
{
    public class PoolableManager
    {
        private readonly Dictionary<Poolable, PoolablePool> m_pools = new Dictionary<Poolable, PoolablePool>();
        private readonly List<Poolable> m_toRemoveCache = new List<Poolable>();
        
        /// <summary>
        /// Get a pooled instance of the given prefab. Creates a pool on first use.
        /// </summary>
        public T Get<T>(T prefab) where T : Poolable
        {
            if (prefab == null)
            {
                Debug.LogError("Get called with null prefab");
                
                PruneDestroyedPrefabs();
                return null;
            }
            
            if (!m_pools.TryGetValue(prefab, out var pool))
            {
                pool = new PoolablePool(prefab);
                m_pools.Add(prefab, pool);
            }

            return pool.Get() as T;
        }

        /// <summary>
        /// Remove pools whose prefab has been destroyed
        /// </summary>
        public void PruneDestroyedPrefabs()
        {
            foreach (var kvp in m_pools)
            {
                if (kvp.Value.IsPrefabDestroyed)
                {
                    kvp.Value.DestroyAllInstances();
                    m_toRemoveCache.Add(kvp.Key);
                }
            }

            for (var i = 0; i < m_toRemoveCache.Count; i++)
            {
                m_pools.Remove(m_toRemoveCache[i]);
            }
            
            m_toRemoveCache.Clear();
        }

        public void ReleaseAll()
        {
            foreach (var (_, pool) in m_pools) {
                pool.ReleaseAll();
            }
        }
    }
}