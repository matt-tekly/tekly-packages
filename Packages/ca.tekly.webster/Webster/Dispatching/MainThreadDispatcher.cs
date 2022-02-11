//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Tekly.Webster.Dispatching
{
	internal class MainThreadDispatcher
	{
		private readonly object m_lock = new object();

		private readonly List<DispatchedAction> m_actions = new List<DispatchedAction>();
		private readonly List<DispatchedResult> m_results = new List<DispatchedResult>();

		private int m_mainThreadId;

		public void Initialize()
		{
			m_mainThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		public void MainThreadUpdate()
		{
			lock (m_lock) {
				for (var index = m_results.Count - 1; index >= 0; index--) {
					var dispatchedResult = m_results[index];
					if (dispatchedResult.State == DispatchedState.Waiting) {
						dispatchedResult.Run();
					} else if (dispatchedResult.IsConsumed) {
						m_results.RemoveAt(index);
					}
				}

				for (var index = m_actions.Count - 1; index >= 0; index--) {
					var action = m_actions[index];
					if (action.State == DispatchedState.Waiting) {
						action.Run();
					} else if (action.IsConsumed) {
						m_actions.RemoveAt(index);
					}
				}
			}
		}
		
		/// <summary>
		/// Dispatches an action to the main thread.
		/// </summary>
		public void Dispatch(Action action, long timeOutMs, int sleepTimeMs)
		{
			var result = Dispatch(action);

			var stopWatch = new Stopwatch();
			stopWatch.Start();

			while (result.State == DispatchedState.Waiting) {
				Thread.Sleep(sleepTimeMs);

				if (stopWatch.ElapsedMilliseconds > timeOutMs) {
					result.Consume();
					throw new Exception("Dispatched Action timed out");
				}
			}

			stopWatch.Stop();

			if (result.Exception != null) {
				UnityEngine.Debug.LogException(result.Exception);
			}
		}
		
		/// <summary>
		/// Dispatches a Func to the main thread.
		/// </summary>
		public T Dispatch<T>(Func<T> func, long timeOutMs, int sleepTimeMs) where T : class
		{
			Func<object> wrappedFunc = () => func();
			return Dispatch(wrappedFunc, timeOutMs, sleepTimeMs) as T;
		}
		
		private DispatchedAction Dispatch(Action action)
		{
			if (Thread.CurrentThread.ManagedThreadId == m_mainThreadId) {
				throw new Exception("Trying to Dispatch action from Main Thread");
			}

			lock (m_lock) {
				var dispatchedAction = new DispatchedAction(action);
				m_actions.Add(dispatchedAction);

				return dispatchedAction;
			}
		}

		private object Dispatch(Func<object> func, long timeOutMs, int sleepTimeMs)
		{
			var result = Dispatch(func);

			var stopWatch = new Stopwatch();
			stopWatch.Start();

			while (result.State == DispatchedState.Waiting) {
				Thread.Sleep(sleepTimeMs);

				if (stopWatch.ElapsedMilliseconds > timeOutMs) {
					result.Consume();
					throw new Exception("Dispatched Result timed out");
				}
			}

			stopWatch.Stop();

			if (result.Exception != null) {
				UnityEngine.Debug.LogException(result.Exception);
			}

			return result.Result;
		}

		
		private DispatchedResult Dispatch(Func<object> action)
		{
			if (Thread.CurrentThread.ManagedThreadId == m_mainThreadId) {
				throw new Exception("Trying to Dispatch action from Main Thread");
			}

			lock (m_lock) {
				var dispatchedResult = new DispatchedResult(action);
				m_results.Add(dispatchedResult);

				return dispatchedResult;
			}
		}
	}
}