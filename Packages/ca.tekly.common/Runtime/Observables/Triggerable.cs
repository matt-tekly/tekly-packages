using System;
using Tekly.Common.Collections;

namespace Tekly.Common.Observables
{
    public interface ITriggerable<out T>
    {
        IDisposable Subscribe(IValueObserver<T> observer);
        IDisposable Subscribe(Action<T> observer);
    }
    
    public class Triggerable<T> : ITriggerable<T>
    {
        private readonly SafeList<IValueObserver<T>> m_observers = new();
        
        public IDisposable Subscribe(IValueObserver<T> observer)
        {
            m_observers.Add(observer);
            return new Unsubscriber<IValueObserver<T>>(observer, m_observers);
        }
        
        public IDisposable Subscribe(Action<T> observer)
        {
            return Subscribe(new ActionObserver<T>(observer));
        }

        public void Emit(T value)
        {
            foreach (var observer in m_observers) {
                observer.Changed(value);
            }
        }
    }
}