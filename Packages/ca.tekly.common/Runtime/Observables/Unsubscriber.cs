// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Tekly.Common.Observables
{
    public class Unsubscriber<T> : IDisposable where T : class
    {
        private readonly T m_observer;
        private readonly IList<T> m_observers;

        public Unsubscriber(T observer, IList<T> observers)
        {
            Assert.IsNotNull(observer);
            
            m_observer = observer;
            m_observers = observers;
        }
            
        public void Dispose()
        {
            m_observers.Remove(m_observer);
        }
    }
}