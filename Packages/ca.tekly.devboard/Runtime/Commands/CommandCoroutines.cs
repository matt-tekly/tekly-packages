using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Tekly.DevBoard.Commands
{
	/// <summary>
	/// Runs the IEnumerators returned by coroutine commands, which need a MonoBehaviour to be driven. One hidden
	/// object, made the first time a coroutine command runs and kept across scenes.
	/// </summary>
	internal sealed class CommandCoroutines : MonoBehaviour
	{
		private static CommandCoroutines s_instance;

		// Still running, so they can be faulted if this object is destroyed under them
		private readonly HashSet<TaskCompletionSource<bool>> m_running = new();

		/// <summary>
		/// Runs routine as a coroutine and completes when it finishes. Cancelling stops it where it is. An exception
		/// from the routine, or from any routine it yields, faults the task.
		/// </summary>
		public static Task Run(IEnumerator routine, CancellationToken cancellationToken)
		{
			if (!Application.isPlaying) {
				return Task.FromException(new InvalidOperationException("Coroutine commands only run in play mode"));
			}

			if (s_instance == null) {
				var gameObject = new GameObject("[DevBoard] Command Coroutines") { hideFlags = HideFlags.HideAndDontSave };
				DontDestroyOnLoad(gameObject);
				s_instance = gameObject.AddComponent<CommandCoroutines>();
			}

			var completion = new TaskCompletionSource<bool>();
			var instance = s_instance;
			instance.m_running.Add(completion);
			var coroutine = instance.StartCoroutine(instance.Drive(routine, completion));

			if (cancellationToken.CanBeCanceled) {
				cancellationToken.Register(() => {
					if (instance != null && completion.TrySetCanceled(cancellationToken)) {
						instance.m_running.Remove(completion);
						instance.StopCoroutine(coroutine);
					}
				});
			}

			return completion.Task;
		}

		/// <summary>
		/// Steps the routine, and any routine it yields, itself: Unity would run a nested one, but then an exception
		/// in it ends the coroutine without a word to the task. Anything else yielded is left to Unity.
		/// </summary>
		private IEnumerator Drive(IEnumerator routine, TaskCompletionSource<bool> completion)
		{
			var routines = new Stack<IEnumerator>();
			routines.Push(routine);

			while (routines.Count > 0) {
				var top = routines.Peek();
				object current;

				try {
					if (!top.MoveNext()) {
						routines.Pop();
						continue;
					}

					current = top.Current;
				} catch (Exception exception) {
					m_running.Remove(completion);
					completion.TrySetException(exception);
					yield break;
				}

				if (current is IEnumerator nested) {
					routines.Push(nested);
					continue;
				}

				yield return current;
			}

			m_running.Remove(completion);
			completion.TrySetResult(true);
		}

		private void OnDestroy()
		{
			if (s_instance == this) {
				s_instance = null;
			}

			// Unity drops the coroutines without a word, so their tasks would wait forever
			foreach (var completion in m_running) {
				completion.TrySetException(new OperationCanceledException("Command coroutines were destroyed before the command finished"));
			}

			m_running.Clear();
		}
	}
}
