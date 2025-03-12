﻿using System;
using System.Collections.Generic;

namespace Tekly.Common.Observables
{
    public class ObservableValue<T> : ITriggerable<T>
    {
        protected static readonly IEqualityComparer<T> s_defaultEqualityComparer = EqualityComparer<T>.Default;

        private Triggerable<Unit> m_modified;
        
        public ITriggerable<Unit> Modified {
            get { return m_modified ??= new Triggerable<Unit>(); }
        }

        public virtual T Value
        {
            get => m_value;
            set {
                if (s_defaultEqualityComparer.Equals(m_value, value)) {
                    return;
                }

                m_value = value;
                Emit(m_value);
            }
        }

        protected T m_value;
        private ObserverLinkedList<T> m_observers;

        public ObservableValue(T value = default)
        {
            m_value = value;
        }

        public IDisposable Subscribe(IValueObserver<T> observer)
        {
            if (m_observers == null) {
                m_observers = new ObserverLinkedList<T>();
            }

            return m_observers.Subscribe(observer, m_value);
        }

        public IDisposable Subscribe(Action<T> observer)
        {
            return Subscribe(new ActionObserver<T>(observer));
        }

        protected virtual void Emit(T value)
        {
            m_observers?.Emit(value);
            m_modified?.Emit(Unit.Default);
        }
    }
}