using Tekly.DevBoard.Nodes;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// Whether and how fast the game runs: time scale, pausing, running in the background, the clocks and the
	/// physics step.
	/// </summary>
	internal static class TimePage
	{
		public const string PATH = "System/Time";

		// What the time scale was before Pause, so Resume goes back to it
		private static float s_scaleBeforePause = 1f;

		public static void Register(object owner)
		{
			DevBoard.Page(PATH, Build, owner: owner);
		}

		private static void Build(Segment page)
		{
			page.Heading("Time Scale");

			// The number, pause and reset above the slider, which has the whole width to itself
			var controls = page.Row().WithCrossAlignment(CrossAlignment.Center);
			controls.FloatInput("Scale", () => Time.timeScale, SetScale).WithFormat("0.00");
			controls.Toggle("Paused", () => Time.timeScale == 0f, SetPaused);
			controls.Button("Reset", () => Time.timeScale = 1f).InteractiveOnly();

			page.Slider(null, 0f, 4f, () => Time.timeScale, SetScale).WithValueInput(false);

			// Whether the game keeps running while the window doesn't have focus
			page.Heading("Background");
			page.Property("Focused", () => Application.isFocused ? "Yes" : "No");
			page.Toggle("Run In Background", () => Application.runInBackground, value => Application.runInBackground = value);

			page.Heading("Clock");
			page.Property("Time", () => Time.time, "{0:F1} s").WithTolerance(0.1f).WithMonospaced();
			page.Property("Unscaled", () => Time.unscaledTime, "{0:F1} s").WithTolerance(0.1f).WithMonospaced();
			page.Property("Real", () => Time.realtimeSinceStartup, "{0:F1} s").WithTolerance(0.1f).WithMonospaced();
			page.Property("Frame", () => Time.frameCount, "{0:N0}").WithMonospaced();

			page.Heading("Physics");
			page.Slider("Fixed Step", 0.005f, 0.05f, () => Time.fixedDeltaTime, value => Time.fixedDeltaTime = value)
				.WithFormat("0.000");
		}

		private static void SetScale(float scale)
		{
			Time.timeScale = Mathf.Max(0f, scale);
		}

		private static void SetPaused(bool paused)
		{
			if (paused) {
				if (Time.timeScale > 0f) {
					s_scaleBeforePause = Time.timeScale;
				}

				Time.timeScale = 0f;
			} else if (Time.timeScale == 0f) {
				Time.timeScale = s_scaleBeforePause;
			}
		}
	}
}
