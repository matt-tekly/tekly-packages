using System;

namespace Tekly.Common.Observables
{
    public struct Unit : IEquatable<Unit>, IComparable<Unit>
    {
        public static readonly Unit Default = new Unit();
        public bool Equals(Unit other) => true;
        public override bool Equals(object obj) => obj is Unit;
        public override int GetHashCode() => 0;
        public int CompareTo(Unit other) => 0;
    }

    public interface ITriggerable<out T>
    {
        IDisposable Subscribe(IValueObserver<T> observer);
        IDisposable Subscribe(Action<T> observer);
    }

    public class Triggerable<T> : ITriggerable<T>, IObserverLinkedList<T>
    {
        private ObserverNode<T> m_root;
        private ObserverNode<T> m_last;

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

            return next;
        }

        public IDisposable Subscribe(Action<T> observer)
        {
            return Subscribe(new ActionObserver<T>(observer));
        }

        public void Emit(T value)
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