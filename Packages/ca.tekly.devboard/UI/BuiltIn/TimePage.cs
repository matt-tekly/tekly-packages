using Tekly.DevBoard.Nodes;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// Time scale, pausing and frame rate limits.
	/// </summary>
	internal static class TimePage
	{
		public const string PATH = "System/Time";

		private static readonly int[] s_frameRates = { -1, 30, 60, 90, 120, 144 };

		private static readonly int[] s_vSyncCounts = { 0, 1, 2 };

		// What the time scale was before Pause, so Resume goes back to it
		private static float s_scaleBeforePause = 1f;

		public static void Register(object owner)
		{
			DevBoard.Page(PATH, Build, owner: owner);
		}

		private static void Build(Segment page)
		{
			page.Heading("Time Scale");

			// The number, pause and reset above the slider, which has the whole width to itself. A scope of their own
			// keeps their labels out of the page's label column
			var controls = page.Row().WithCrossAlignment(CrossAlignment.Center).WithWidthGroupScope();
			controls.FloatInput("Scale", () => Time.timeScale, SetScale).WithFormat("0.00");
			controls.Toggle("Paused", () => Time.timeScale == 0f, SetPaused);
			controls.Button("Reset", () => Time.timeScale = 1f).InteractiveOnly();

			page.Slider(null, 0f, 4f, () => Time.timeScale, SetScale).WithValueInput(false);

			page.Heading("Clock");
			page.Property("Time", () => Time.time, 0.1f, "{0:F1} s").WithMonospaced();
			page.Property("Unscaled", () => Time.unscaledTime, 0.1f, "{0:F1} s").WithMonospaced();
			page.Property("Real", () => Time.realtimeSinceStartup, 0.1f, "{0:F1} s").WithMonospaced();
			page.Property("Frame", () => Time.frameCount, "{0:N0}").WithMonospaced();

			page.Heading("Frame Rate");
			// A rate that isn't one of the choices, set by the game or typed in Custom, shows as no choice. Custom
			// takes any rate, -1 for the platform default
			page.Dropdown("Target", s_frameRates, rate => rate < 0 ? "Default" : rate.ToString(),
				() => Application.targetFrameRate, rate => Application.targetFrameRate = rate);
			page.IntInput("Custom", () => Application.targetFrameRate, rate => Application.targetFrameRate = rate);
			page.Dropdown("VSync", s_vSyncCounts, count => count == 0 ? "Off" : count == 1 ? "Every Frame" : "Every Second Frame",
				() => Mathf.Clamp(QualitySettings.vSyncCount, 0, 2), count => QualitySettings.vSyncCount = count);
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
