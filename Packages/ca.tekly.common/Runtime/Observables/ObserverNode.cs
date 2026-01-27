using System;
using System.Threading;
using UnityEngine;

namespace Tekly.Common.Observables
{
    internal interface IObserverLinkedList<T>
    {
        void UnsubscribeNode(ObserverNode<T> node);
    }

    internal sealed class ObserverLinkedList<T> : IObserverLinkedList<T>
    {
        private ObserverNode<T> m_root;
        private ObserverNode<T> m_last;

        public IDisposable Subscribe(IValueObserver<T> observer, T currentValue, bool changesOnly = false)
        {
            var next = new ObserverNode<T>(this, observer);

            if (m_root == null) {
                m_root = m_last = next;
            } else {
                m_last.Next = next;
                next.Previous = m_last;
                m_last = next;
            }

            try {
                if (!changesOnly)
                {
                    observer.Changed(currentValue);    
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
            
            return next;
        }

        public void Emit(T value)
        {
            var node = m_root;

            while (node != null) {
                var next = node.Next;
                try {
                    node.Changed(value);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
                
                node = next;
            }
        }

        public void UnsubscribeNode(ObserverNode<T> node)
        {
            if (node == m_root) {
                m_root = node.Next;
            }

            if (node == m_last) {
                m_last = node.Previous;
            }

            if (node.Previous != null) {
                node.Previous.Next = node.Next;
            }

            if (node.Next != null) {
                node.Next.Previous = node.Previous;
            }
            
            node.Previous = null;
            node.Next = null;
        }
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