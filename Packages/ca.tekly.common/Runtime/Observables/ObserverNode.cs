using System;
using System.Threading;

namespace Tekly.Common.Observables
{
    internal interface IObserverLinkedList<T>
    {
        void UnsubscribeNode(ObserverNode<T> node);
    }
    
    internal sealed class ObserverNode<T> : IDisposable
    {
        public ObserverNode<T> Previous { get; internal set; }
        public ObserverNode<T> Next { get; internal set; }

        private readonly IValueObserver<T> m_observer;
        private IObserverLinkedList<T> m_list;

        public ObserverNode(IObserverLinkedList<T> list, IValueObserver<T> observer)
        {
            m_list = list;
            m_observer = observer;
        }

        public void Changed(T value)
        {
            m_observer.Changed(value);
        }

        public void Dispose()
        {
            var sourceList = Interlocked.Exchange(ref m_list, null);
            sourceList?.UnsubscribeNode(this);
        }
    }
}