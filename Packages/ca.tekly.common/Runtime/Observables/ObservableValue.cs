using System;
using System.Collections.Generic;

namespace Tekly.Common.Observables
{
	public interface IObservableValue<T> : ITriggerable<T>
	{
		T Value { get; }
	}

	public class ObservableValue<T> : IObservableValue<T>
	{
		protected static readonly IEqualityComparer<T> s_defaultEqualityComparer = EqualityComparer<T>.Default;

		private Triggerable<Unit> m_modified;

		public ITriggerable<Unit> Modified => m_modified ??= new Triggerable<Unit>();

		public virtual T Value {
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

		/// <summary>
		/// Subscribes to the value and invokes the observer immediately
		/// </summary>
		public IDisposable Subscribe(IValueObserver<T> observer)
		{
			return SubscribeChanges(observer);
		}

		/// <summary>
		/// Subscribes to the value and invokes the observer immediately
		/// </summary>
		public IDisposable Subscribe(Action<T> observer)
		{
			return SubscribeChanges(new ActionObserver<T>(observer));
		}

		/// <summary>
		/// Subscribes to the value but doesn't invoke the observer immediately
		/// </summary>
		public IDisposable SubscribeChanges(Action<T> observer)
		{
			return SubscribeChanges(new ActionObserver<T>(observer), true);
		}

		private IDisposable SubscribeChanges(IValueObserver<T> observer, bool changesOnly = false)
		{
			if (m_observers == null) {
				m_observers = new ObserverLinkedList<T>();
			}

			return m_observers.Subscribe(observer, m_value, changesOnly);
		}

		protected virtual void Emit(T value)
		{
			m_observers?.Emit(value);
			m_modified?.Emit(Unit.Default);
		}
	}
}