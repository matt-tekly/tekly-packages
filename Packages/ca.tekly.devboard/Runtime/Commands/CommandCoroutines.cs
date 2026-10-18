using System;
using System.Collections;
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

		/// <summary>
		/// Runs routine as a coroutine and completes when it finishes. Cancelling stops it where it is. An exception
		/// from the routine itself faults the task; one from a routine it yields is logged by Unity and ends it.
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
			var coroutine = s_instance.StartCoroutine(s_instance.Drive(routine, completion));

			if (cancellationToken.CanBeCanceled) {
				cancellationToken.Register(() => {
					if (s_instance != null && completion.TrySetCanceled(cancellationToken)) {
						s_instance.StopCoroutine(coroutine);
					}
				});
			}

			return completion.Task;
		}

		private IEnumerator Drive(IEnumerator routine, TaskCompletionSource<bool> completion)
		{
			while (true) {
				object current;

				try {
					if (!routine.MoveNext()) {
						break;
					}

					current = routine.Current;
				} catch (Exception exception) {
					completion.TrySetException(exception);
					yield break;
				}

				yield return current;
			}

			completion.TrySetResult(true);
		}

		private void OnDestroy()
		{
			if (s_instance == this) {
				s_instance = null;
			}
		}
	}
}
