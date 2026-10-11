using System.Text;
using Tekly.DevBoard.Commands;
using UnityEngine;

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
	}
}
