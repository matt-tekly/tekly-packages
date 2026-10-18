using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Tekly.DevBoard.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// Commands DevBoard brings itself.
	/// </summary>
	internal static class BuiltInCommands
	{
		[DevCommand("help", "Lists the commands, or those containing filter")]
		private static void Help(CommandContext context, string filter = "")
		{
			foreach (var command in DevBoard.Instance.Commands.All) {
				if (filter.Length > 0 && command.Name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) {
					continue;
				}

				var line = new StringBuilder(command.Usage);

				if (command.Description != null) {
					line.Append("  <alpha=#80>").Append(command.Description).Append("<alpha=#FF>");
				}

				context.Log(line.ToString());
			}
		}

		[DevCommand("time scale", "Sets Time.timeScale. 1 is normal speed")]
		private static string TimeScale(float scale = 1f)
		{
			Time.timeScale = Mathf.Max(0f, scale);
			return $"Time scale {Time.timeScale}";
		}

		[DevCommand("target fps", "Sets Application.targetFrameRate. -1 is the platform default")]
		private static string TargetFps(int fps = -1)
		{
			Application.targetFrameRate = fps;
			return $"Target frame rate {Application.targetFrameRate}";
		}

		/// <summary>
		/// For spacing out a line of commands: "time scale 0.1; wait 2; time scale 1". Real seconds, so it ends
		/// whatever the time scale.
		/// </summary>
		[DevCommand("wait", "Waits seconds of real time before the next command")]
		private static async Awaitable Wait(CommandContext context, float seconds = 1f)
		{
			var end = Time.realtimeSinceStartup + Mathf.Max(0f, seconds);

			while (Time.realtimeSinceStartup < end) {
				await Awaitable.NextFrameAsync(context.CancellationToken);
			}
		}

		[DevCommand("wait frames", "Waits a number of frames before the next command")]
		private static IEnumerator WaitFrames(int frames = 1)
		{
			for (var i = 0; i < frames; i++) {
				yield return null;
			}
		}
	}
}
