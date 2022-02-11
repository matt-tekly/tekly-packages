// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Tekly.Common.Utils
{
    public abstract class ObjectPoolBase<TValue> where TValue : class
    {
        private readonly Stack<TValue> m_stack = new Stack<TValue>();
        private int m_activeCount;

#if TK_MULTITHREADING
        protected readonly object _locker = new object();
#endif
        public int NumTotal => NumInactive + NumActive;

        public int NumActive
        {
            get
            {
#if TK_MULTITHREADING
                lock (_locker)
#endif
                {
                    return m_activeCount;
                }
            }
        }

        public int NumInactive
        {
            get
            {
#if TK_MULTITHREADING
                lock (_locker)
#endif
                {
                    return m_stack.Count;
                }
            }
        }

        public Type ItemType => typeof(TValue);

        public TValue Spawn()
        {
#if TK_MULTITHREADING
            lock (_locker)
#endif
            {
                return SpawnInternal();
            }
        }

        public void Resize(int desiredPoolSize)
        {
#if TK_MULTITHREADING
            lock (_locker)
#endif
            {
                ResizeInternal(desiredPoolSize);
            }
        }

        // We assume here that we're in a lock
        private void ResizeInternal(int desiredPoolSize)
        {
            Assert.IsTrue(desiredPoolSize >= 0, "Attempted to resize the pool to a negative amount");

            while (m_stack.Count > desiredPoolSize)
            {
                m_stack.Pop();
            }

            while (desiredPoolSize > m_stack.Count)
            {
                m_stack.Push(Allocate());
            }

            Assert.AreEqual(m_stack.Count, desiredPoolSize);
        }
        
        public void ClearActiveCount()
        {
#if TK_MULTITHREADING
            lock (_locker)
#endif
            {
                m_activeCount = 0;
            }
        }

        public void Clear()
        {
            Resize(0);
        }

        public void ShrinkBy(int numToRemove)
        {
#if TK_MULTITHREADING
            lock (_locker)
#endif
            {
                ResizeInternal(m_stack.Count - numToRemove);
            }
        }

        public void ExpandBy(int numToAdd)
        {
#if TK_MULTITHREADING
            lock (_locker)
#endif
            {
                ResizeInternal(m_stack.Count + numToAdd);
            }
        }

        // We assume here that we're in a lock
        protected TValue SpawnInternal()
        {
            var element = m_stack.Count == 0 ? Allocate() : m_stack.Pop();

            m_activeCount++;
            return element;
        }
        
        public void Recycle(TValue element)
        {
            Recycled(element);

#if TK_MULTITHREADING
            lock (_locker)
#endif
            {
                Assert.IsFalse(m_stack.Contains(element), "Attempted to recycle element twice!");

                m_activeCount--;
                m_stack.Push(element);
            }
        }

        protected abstract TValue Allocate();
        protected abstract void Recycled(TValue element);
    }
}