using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tekly.Common.Utils
{
    public class ArraysPool<TValue>
    {
        private readonly Dictionary<int, ArrayPool> m_arrayPools = new Dictionary<int, ArrayPool>();
        private readonly object m_lock = new object();

        public TValue[] Get(int size)
        {
            lock (m_lock) {
                if (!m_arrayPools.TryGetValue(size, out var pool)) {
                    pool = new ArrayPool(size);
                    m_arrayPools[size] = pool;
                }

                return pool.Get();
            }
        }

        public void Return(TValue[] value)
        {
            lock (m_lock) {
                if (m_arrayPools.TryGetValue(value.Length, out var pool)) {
                    pool.Return(value);
                } else {
                    Debug.LogError("Trying to return Array that wasn't in ArrayPool");
                }
            }
        }
        
        private class ArrayPool
        {
            private readonly Stack<TValue[]> m_stack = new Stack<TValue[]>();
            private readonly int m_size;

            public ArrayPool(int size)
            {
                m_size = size;
            }

            public TValue[] Get()
            {
                return m_stack.Count == 0 ? new TValue[m_size] : m_stack.Pop();
            }

            public void Return(TValue[] value)
            {
                Assert.IsFalse(m_stack.Contains(value), "Attempted to return value twice!");
                m_stack.Push(value);

                var length = value.Length;
                for (var i = 0; i < length; i++) {
                    value[i] = default;
                }
            }
        }
    }
}