using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tekly.Common.Poolables
{
    internal sealed class PoolablePool
    {
        public bool IsPrefabDestroyed => m_prefab == null;
        
        private readonly Poolable m_prefab;
        private readonly Transform m_root;

        private readonly Stack<Poolable> m_inactive = new Stack<Poolable>();
        private readonly HashSet<Poolable> m_all = new HashSet<Poolable>();

        public PoolablePool(Poolable prefab)
        {
            m_prefab = prefab;

            var rootGo = new GameObject($"[Pool] {prefab.name}");
            Object.DontDestroyOnLoad(rootGo);
            m_root = rootGo.transform;
        }
        
        /// <summary>
        /// Returns an inactive instance of Poolable
        /// </summary>
        /// <returns></returns>
        public Poolable Get()
        {
            if (m_prefab == null)
            {
                return null;
            }

            Poolable instance = null;

            while (m_inactive.Count > 0 && instance == null)
            {
                var candidate = m_inactive.Pop();
                if (candidate != null && candidate.PoolableStatus != PoolableStatus.Destroyed)
                {
                    instance = candidate;
                }
            }

            if (instance == null)
            {
                instance = Object.Instantiate(m_prefab);
                instance.SetPool(this);
                m_all.Add(instance);
            }

            instance.PoolableStatus = PoolableStatus.InUse;
            instance.OnGet();
            
            return instance;
        }

        internal void Release(Poolable instance)
        {
            if (instance == null || instance.PoolableStatus == PoolableStatus.Destroyed)
            {
                return;
            }

            if (instance.PoolableStatus == PoolableStatus.InPool)
            {
                return;
            }

            instance.gameObject.SetActive(false);

            var transform = instance.transform;
            if (transform.parent != m_root)
            {
                transform.SetParent(m_root, false);
            }

            instance.PoolableStatus = PoolableStatus.InPool;
            m_inactive.Push(instance);
        }

        public void ReleaseAll()
        {
            foreach (var poolable in m_all.ToArray()) {
                if (poolable.PoolableStatus == PoolableStatus.InUse) {
                    poolable.Release();
                }
            }
        }

        internal void OnInstanceDestroyed(Poolable instance)
        {
            m_all.Remove(instance);
            // If it was in _inactive, it’ll just show up as null / Destroyed and be skipped in Get().
        }

        public void DestroyAllInstances()
        {
            foreach (var instance in m_all)
            {
                if (instance != null)
                {
                    instance.PoolableStatus = PoolableStatus.Destroyed;
                    Object.Destroy(instance.gameObject);
                }
            }

            m_all.Clear();
            m_inactive.Clear();

            if (m_root != null)
            {
                Object.Destroy(m_root.gameObject);
            }
        }
    }
}