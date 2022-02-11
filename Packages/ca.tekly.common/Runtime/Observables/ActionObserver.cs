using System;

namespace Tekly.Common.Observables
{
    public class ActionObserver<T> : IValueObserver<T>
    {
        private readonly Action<T> m_observer;
        
        public ActionObserver(Action<T> observer)
        {
            m_observer = observer;
        }
        
        public void Changed(T value)
        {
            m_observer.Invoke(value);
        }
    }
}