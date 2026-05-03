using System;
using System.Collections.Generic;
using Tekly.Common.Observables;
using UnityEngine.Assertions;

namespace Tekly.Common.Utils
{
	public class Latch<T> where T : class
	{
		public IObservableValue<bool> IsHeld => m_isHeld;
		
		private readonly ObservableValue<bool> m_isHeld = new ObservableValue<bool>();
		private readonly List<T> m_holders = new List<T>();

		public IDisposable HoldScope(T holder)
		{
			Assert.IsNotNull(holder);
			
			m_holders.Add(holder);
			m_isHeld.Value = true;
			return new Token(this, holder);
		}
		
		public void Hold(T holder)
		{
			Assert.IsNotNull(holder);
			
			m_holders.Add(holder);
			m_isHeld.Value = true;
		}

		public void Release(T holder)
		{
			m_holders.Remove(holder);
			m_isHeld.Value = m_holders.Count > 0;
		}
		
		private sealed class Token : IDisposable
		{
			private Latch<T> m_latch;
			private T m_holder;

			public Token(Latch<T> latch, T holder) {
				m_latch = latch;
				m_holder = holder;
			}

			public void Dispose() {
				if (m_latch == null) {
					return;
				}
				
				m_latch.Release(m_holder);
				m_latch = null;
				m_holder = default;
			}
		}
	}

	public class Latch : Latch<object> { }
}