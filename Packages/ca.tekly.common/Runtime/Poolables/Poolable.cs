using UnityEngine;
using UnityEngine.Assertions;

namespace Tekly.Common.Poolables
{
    /// <summary>
    /// Represents a GameObject that can be pooled.
    /// </summary>
    public class Poolable : MonoBehaviour
    {
        public PoolableStatus PoolableStatus { get; internal set; } = PoolableStatus.None;
        public uint Generation { get; private set; }
        
        public bool IsDestroyed => PoolableStatus == PoolableStatus.Destroyed;
        public bool IsInPool => PoolableStatus == PoolableStatus.InPool;
        
        private PoolablePool m_pool;

        internal void SetPool(PoolablePool pool)
        {
            Assert.IsNull(m_pool, "Poolable object being assigned to a pool even though it already has one.");
            m_pool = pool;
        }

        /// <summary>
        /// Return this instance to its pool. If no pool is set, it will be destroyed immediately.
        /// </summary>
        public void Release()
        {
            if (IsDestroyed) {
                return;
            }
            
            if (IsInPool) {
                Debug.LogWarning($"Poolable [{name}] released while already InPool.", this);
                return;
            }

            Generation++;
            OnReleased();

            if (m_pool != null) {
                m_pool.Release(this);
                return;
            }

            // No pool fallback.
            PoolableStatus = PoolableStatus.Destroyed;
            Destroy(gameObject);
        }

        /// <summary>
        /// Called when the Pool is giving this object out.
        /// </summary>
        public virtual void OnGet()
        {
            
        }
        
        /// <summary>
        /// Called when this object is released regardless of it is pooled.
        /// If the object is regularly destroyed this isn't called.
        /// </summary>
        protected virtual void OnReleased()
        {
            
        }

        protected virtual void OnDestroy()
        {
            if (IsDestroyed) {
                return;
            }

            PoolableStatus = PoolableStatus.Destroyed;

            if (m_pool != null) {
                m_pool.OnInstanceDestroyed(this);
                m_pool = null;
            }
        }
    }
}