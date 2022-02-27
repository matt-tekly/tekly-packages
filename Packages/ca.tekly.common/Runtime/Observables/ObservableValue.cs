using System;
using System.Collections.Generic;

namespace Tekly.Common.Observables
{
    public class ObservableValue<T> : ITriggerable<T>, IObserverLinkedList<T>
    {
        private static readonly IEqualityComparer<T> s_defaultEqualityComparer = EqualityComparer<T>.Default;

        public T Value
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

        private ObserverNode<T> m_root;
        private ObserverNode<T> m_last;

        public ObservableValue(T value)
        {
            m_value = value;
        }
        
        public ObservableValue() { }

        public IDisposable Subscribe(IValueObserver<T> observer)
        {
            var next = new ObserverNode<T>(this, observer);
            
            if (m_root == null) {
                m_root = m_last = next;
            } else {
                m_last.Next = next;
                next.Previous = m_last;
                m_last = next;
            }

            observer.Changed(m_value);
            
            return next;
        }

        public IDisposable Subscribe(Action<T> observer)
        {
            return Subscribe(new ActionObserver<T>(observer));
        }

        private void Emit(T value)
        {
            var node = m_root;
            while (node != null) {
                node.Changed(value);
                node = node.Next;
            }
        }

        void IObserverLinkedList<T>.UnsubscribeNode(ObserverNode<T> node)
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
        }
    }
}