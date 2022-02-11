//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Threading;

namespace Tekly.Webster.Dispatching
{
	public class DispatchedAction
	{
		private readonly Action m_action;

		private int m_isConsumed;

		public DispatchedAction(Action action)
		{
			m_action = action;
			State = DispatchedState.Waiting;
		}

		public DispatchedState State { get; private set; }

		public bool IsConsumed
		{
			get => Interlocked.CompareExchange(ref m_isConsumed, 1, 1) == 1;
			private set => Interlocked.Exchange(ref m_isConsumed, value ? 1 : 0);
		}

		public Exception Exception { get; private set; }

		public void Run()
		{
			try {
				m_action();
				State = DispatchedState.Success;
			} catch (Exception e) {
				Exception = e;
				State = DispatchedState.Error;
			}
		}

		public void Consume()
		{
			IsConsumed = true;
		}
	}
}