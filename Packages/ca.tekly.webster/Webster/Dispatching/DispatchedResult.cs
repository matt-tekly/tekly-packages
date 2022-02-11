//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Threading;

namespace Tekly.Webster.Dispatching
{
	public enum DispatchedState
	{
		Waiting,
		Success,
		Error
	}

	public class DispatchedResult
	{
		private readonly Func<object> m_action;
		private int m_isConsumed;

		public DispatchedResult(Func<object> action)
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

		public object Result { get; private set; }

		public Exception Exception { get; private set; }

		public void Run()
		{
			try {
				Result = m_action();
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