using System;
using System.Threading.Tasks;
using Tekly.Logging;
using UnityEngine;

namespace Tekly.Extensions.Utils
{
	public static class AsyncExtensions
	{
		public static async void FireAndForget(this Task task)
		{
			try {
				await task;
			} catch (Exception e) {
				Debug.LogException(e);
			}
		}
        
		public static async void FireAndForget(this Task task, TkLogger logger)
		{
			try {
				await task;
			} catch (Exception e) {
				logger.Exception(e, "Exception during FireAndForget");
			}
		}
	}
}