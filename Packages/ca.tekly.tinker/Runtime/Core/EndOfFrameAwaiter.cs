using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Tekly.Common.LifeCycles;
using UnityEngine;

namespace Tekly.Tinker.Core
{
	public class EndOfFrameAwaiter : INotifyCompletion
	{
		public bool IsCompleted => false;

		public void OnCompleted(Action continuation)
		{
			LifeCycle.Instance.StartCoroutine(WaitForEndOfFrameCoroutine(continuation));
		}

		public void GetResult() { }

		private IEnumerator WaitForEndOfFrameCoroutine(Action continuation)
		{
			yield return new WaitForEndOfFrame();
			continuation?.Invoke();
		}

		public EndOfFrameAwaiter GetAwaiter() => this;
	}
}