using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Tekly.Common.LifeCycles;
using UnityEngine;

namespace Tekly.Tinker.Core
{
	public class EndOfFrameAwaiter : INotifyCompletion
	{
		private Action m_continuation;
		
		public bool IsCompleted => false;

		public void OnCompleted(Action continuation)
		{
			m_continuation = continuation;
			LifeCycle.Instance.StartCoroutine(WaitForEndOfFrameCoroutine());
		}

		public void GetResult() { }

		private IEnumerator WaitForEndOfFrameCoroutine()
		{
			yield return new WaitForEndOfFrame();
			m_continuation?.Invoke();
		}

		public EndOfFrameAwaiter GetAwaiter() => this;
	}
}