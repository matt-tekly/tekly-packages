#if UNITY_EDITOR && TINKER_ENABLED_EDITOR
#define TINKER_ENABLED
#endif

#if TINKER_ENABLED
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Tekly.Tinker.Core
{
	public class EndOfFrameAwaiter : INotifyCompletion
	{
		public bool IsCompleted => false;

		public void OnCompleted(Action continuation)
		{
			TinkerServer.Instance.StartCoroutine(WaitForEndOfFrameCoroutine(continuation));
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
#endif