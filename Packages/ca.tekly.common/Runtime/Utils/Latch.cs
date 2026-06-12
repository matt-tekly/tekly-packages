using System;
using System.Collections.Generic;
using Tekly.Common.Observables;
using UnityEngine.Assertions;

namespace Tekly.Common.Utils
{
	public class Latch : IDisposable
	{
		public IObservableValue<bool> IsHeld => m_isHeld;

		private readonly ObservableValue<bool> m_isHeld = new();
		private readonly List<object> m_holders = new();
		private readonly IDisposable m_parentSubscription;
		private readonly object m_parentHolder;
		private bool m_isHoldingParent;

		public Latch() { }

		public Latch(Latch parent)
		{
			Assert.IsNotNull(parent);

			m_parentHolder = new object();

			m_parentSubscription = parent.IsHeld.Subscribe(isHeld => {
				if (isHeld == m_isHoldingParent) {
					return;
				}

				m_isHoldingParent = isHeld;

				if (isHeld) {
					Hold(m_parentHolder);
				} else {
					Release(m_parentHolder);
				}
			});
		}

		public IDisposable HoldScope(object holder)
		{
			Hold(holder);
			return new Token(this, holder);
		}

		public void Hold(object holder)
		{
			Assert.IsNotNull(holder);

			m_holders.Add(holder);
			m_isHeld.Value = true;
		}

		public void Release(object holder)
		{
			Assert.IsNotNull(holder);
			
			m_holders.Remove(holder);
			m_isHeld.Value = m_holders.Count > 0;
		}

		public void Dispose()
		{
			m_parentSubscription?.Dispose();

			if (m_isHoldingParent) {
				m_isHoldingParent = false;
				Release(m_parentHolder);
			}
		}

		private sealed class Token : IDisposable
		{
			private Latch m_latch;
			private object m_holder;

			public Token(Latch latch, object holder)
			{
				m_latch = latch;
				m_holder = holder;
			}

			public void Dispose()
			{
				if (m_latch == null) {
					return;
				}

				m_latch.Release(m_holder);
				m_latch = null;
				m_holder = null;
			}
		}
	}
}