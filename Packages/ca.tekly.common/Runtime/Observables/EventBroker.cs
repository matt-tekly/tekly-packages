using System;
using System.Collections.Generic;

namespace Tekly.Common.Observables
{
    public class EventBroker
    {
        private readonly Dictionary<Type, object> m_triggerables = new Dictionary<Type, object>();
        
        public void Emit<T>(T value)
        {
            var observableValue = GetTriggerable<T>();
            observableValue.Emit(value);
        }

        public IDisposable Subscribe<T>(Action<T> observer)
        {
            return Subscribe(new ActionObserver<T>(observer));
        }

        public IDisposable Subscribe<T>(IValueObserver<T> observer)
        {
            var observableValue = GetTriggerable<T>();
            return observableValue.Subscribe(observer);
        }

        private Triggerable<T> GetTriggerable<T>()
        {
            if (!m_triggerables.TryGetValue(typeof(T), out var observable)) {
                observable = new Triggerable<T>();
                m_triggerables.Add(typeof(T), observable);
            }

            return observable as Triggerable<T>;
        }
    }
}